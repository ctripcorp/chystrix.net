using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

using CHystrix.Utils;

namespace CHystrix
{
    public abstract class SemaphoreIsolationCommand<T> : HystrixCommandBase<T>, ISemaphoreIsolation<T>
    {
        private readonly IsolationSemaphore ExecutionSemaphore;
        private readonly IsolationSemaphore FallbackExecutionSemaphore;

        protected SemaphoreIsolationCommand()
            : this(null, null, null, null, false)
        {
        }

        internal SemaphoreIsolationCommand(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config, bool hasFallback)
            : this(null, commandKey, groupKey, domain, config, hasFallback)
        {
        }

        internal SemaphoreIsolationCommand(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config, bool hasFallback)
            : base(instanceKey, commandKey, groupKey, domain, config)
        {
            ExecutionSemaphore = ExecutionSemaphores.GetOrAdd(Key, key => new IsolationSemaphore(ConfigSet.CommandMaxConcurrentCount));
            if (hasFallback || HasFallback)
                FallbackExecutionSemaphore = FallbackExecutionSemaphores.GetOrAdd(Key, key => new IsolationSemaphore(ConfigSet.FallbackMaxConcurrentCount));
        }

        internal override IsolationModeEnum IsolationMode
        {
            get { return IsolationModeEnum.SemaphoreIsolation; }
        }

        public T Run()
        {
            if (Status == CommandStatusEnum.NotStarted)
            {
                if (Monitor.TryEnter(ExecutionLock))
                {
                    try
                    {
                        if (Status == CommandStatusEnum.NotStarted)
                        {
                            Stopwatch stopwatch = null;
                            try
                            {
                                stopwatch = new Stopwatch();
                                stopwatch.Start();
                                return ExecuteWithSemaphoreIsolation();
                            }
                            catch (Exception ex)
                            {
                                if (HasFallback)
                                    return ExecuteFallback(ex);
                                throw;
                            }
                            finally
                            {
                                if (stopwatch != null)
                                    stopwatch.Stop();
                                Metrics.MarkTotalExecutionLatency(stopwatch.ElapsedMilliseconds);
                            }
                        }
                    }
                    catch
                    {
                        Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);
                        throw;
                    }
                    finally
                    {
                        Monitor.Exit(ExecutionLock);
                    }
                }
            }

            string message = "The command has been started or finished. A command can be only run once.";
            Log.Log(LogLevelEnum.Error, message, GetLogTagInfo().AddLogTagData("FXD303018"));
            throw new InvalidOperationException(message);
        }

        private T ExecuteWithSemaphoreIsolation()
        {
            if (!CircuitBreaker.AllowRequest())
            {
                string errorMessage = "Circuit Breaker is open. Execution was short circuited.";
                Log.Log(LogLevelEnum.Error, errorMessage, GetLogTagInfo().AddLogTagData("FXD303019"));
                Status = CommandStatusEnum.ShortCircuited;
                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ShortCircuited);
                throw new HystrixException(FailureTypeEnum.ShortCircuited, this.GetType(), Key, errorMessage);
            }

            if (ExecutionSemaphore.TryAcquire())
            {
                Stopwatch stopwatch = new Stopwatch();
                try
                {
                    stopwatch.Start();
                    Status = CommandStatusEnum.Started;
                    T result = Execute();
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds > ConfigSet.CommandTimeoutInMilliseconds)
                    {
                        Status = CommandStatusEnum.Timeout;
                        Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Timeout);
                        Log.Log(LogLevelEnum.Warning, string.Format("HystrixCommand execution timeout: {0}ms.", stopwatch.ElapsedMilliseconds),
                            GetLogTagInfo().AddLogTagData("FXD303020"));
                    }
                    else
                    {
                        Status = CommandStatusEnum.Success;
                        Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Success);
                        CircuitBreaker.MarkSuccess();
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (ex.IsBadRequestException())
                    {
                        Status = CommandStatusEnum.Failed;
                        Metrics.MarkExecutionEvent(CommandExecutionEventEnum.BadRequest);
                        if (ConfigSet.LogExecutionError)
                            Log.Log(LogLevelEnum.Error, "HystrixCommand request is bad.", ex, GetLogTagInfo().AddLogTagData("FXD303021"));
                        throw;
                    }

                    Status = CommandStatusEnum.Failed;
                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Failed);
                    if (ConfigSet.LogExecutionError)
                        Log.Log(LogLevelEnum.Error, "HystrixCommand execution failed.", ex, GetLogTagInfo().AddLogTagData("FXD303022"));
                    throw;
                }
                finally
                {
                    ExecutionSemaphore.Release();

                    stopwatch.Stop();
                    Metrics.MarkExecutionLatency(stopwatch.ElapsedMilliseconds);
                }
            }
            else
            {
                Status = CommandStatusEnum.Rejected;
                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Rejected);
                string errorMessage = "HystrixCommand execution was rejected.";
                Log.Log(LogLevelEnum.Error, errorMessage, GetLogTagInfo().AddLogTagData("FXD303023"));
                throw new HystrixException(FailureTypeEnum.SemaphoreIsolationRejected, GetType(), Key, errorMessage);
            }
        }

        private T ExecuteFallback(Exception cause)
        {
            if (cause is HystrixException && cause.InnerException != null)
                cause = cause.InnerException;

            if (FallbackExecutionSemaphore.TryAcquire())
            {
                try
                {
                    T result = ToIFallback().GetFallback();
                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.FallbackSuccess);
                    Status = CommandStatusEnum.FallbackSuccess;
                    Log.Log(LogLevelEnum.Warning, "HystrixCommand execution failed, use fallback instead.");
                    return result;
                }
                catch (Exception ex)
                {
                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.FallbackFailed);
                    Status = CommandStatusEnum.FallbackFailed;
                    if (ConfigSet.LogExecutionError)
                        Log.Log(LogLevelEnum.Error, "HystrixCommand fallback execution failed.", ex, GetLogTagInfo().AddLogTagData("FXD303024"));
                    throw;
                }
                finally
                {
                    FallbackExecutionSemaphore.Release();
                }
            }
            else
            {
                Status = CommandStatusEnum.FallbackRejected;
                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.FallbackRejected);
                string errorMessage = "HystrixCommand fallback execution was rejected.";
                Log.Log(LogLevelEnum.Error, errorMessage, GetLogTagInfo().AddLogTagData("FXD303025"));
                throw new HystrixException(FailureTypeEnum.FallbackRejected, GetType(), Key, errorMessage, cause);
            }
        }
    }
}
