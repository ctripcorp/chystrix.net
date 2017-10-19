using CHystrix.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using CHystrix.Threading;

namespace CHystrix
{
    public abstract class ThreadIsolationCommand<T> : HystrixCommandBase<T>, IThreadIsolation<T>
    {
        #region Fields

        private readonly IsolationSemaphore FallbackExecutionSemaphore;

        #endregion

        #region Construction

        protected ThreadIsolationCommand()
            :this(null,null,null,null)
        {
          
        }

        internal ThreadIsolationCommand(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config, bool hasFallback = false)
            : this(null, commandKey, groupKey, domain, config, hasFallback)
        {
        }

        internal ThreadIsolationCommand(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config, bool hasFallback)
            : base(instanceKey, commandKey, groupKey, domain, config)
        {
            if (hasFallback || HasFallback)
                FallbackExecutionSemaphore = FallbackExecutionSemaphores.GetOrAdd(Key, key => new IsolationSemaphore(ConfigSet.FallbackMaxConcurrentCount));

            ConfigSet.SubcribeConfigChangeEvent((cf) =>
            {
                this.UpdateMaxConcurrentCount(cf.CommandMaxConcurrentCount);
                this.UpdateCommandTimeoutInMilliseconds(cf.CommandTimeoutInMilliseconds);

            });
        }

        #endregion

        #region Properties

        internal override IsolationModeEnum IsolationMode
        {
            get { return IsolationModeEnum.ThreadIsolation; }
        }

        #endregion

        #region IThreadIsolation

        public Task<T> RunAsync()
        {
            return ExecuteWithThreadPool();
        }

        #endregion

        #region private Methods

        private Task<T> GetFallBack()
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            
            try
            {
                tcs.SetResult(ExecuteFallback());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);

            }

         

            return tcs.Task;
        }

        private Task<T> ExecuteWithThreadPool()
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            try
            {
                if (!CircuitBreaker.AllowRequest())
                {
                    var shortCircuMsg = "Circuit Breaker is open. Execution was short circuited.";
                    Log.Log(LogLevelEnum.Error, shortCircuMsg, GetLogTagInfo().AddLogTagData("FXD303033"));
                    Status = CommandStatusEnum.ShortCircuited;
                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ShortCircuited);
                    throw new HystrixException(FailureTypeEnum.ShortCircuited, this.GetType(), Key, shortCircuMsg);
                }

                Status = CommandStatusEnum.Started;

                var newTask = this.QueueWorkItem(this.Execute,
                (o, e) =>
                {
                    var t = o as CWorkItem<T>;
                    var needFallback = false;
                    
                    try
                    {

                        if (t.IsCompleted || t.IsCanceled)
                        {
                            Metrics.MarkTotalExecutionLatency(t.ExeuteMilliseconds);
                            Metrics.MarkExecutionLatency(t.RealExecuteMilliseconds);
                        }

                        switch (e.Status)
                        {
                            case CTaskStatus.WaitingToRun:
                                break;
                            case CTaskStatus.Running:
                                Status = CommandStatusEnum.Started;
                                break;
                            case CTaskStatus.RanToCompletion:
                                Status = CommandStatusEnum.Success;
                                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Success);
                                CircuitBreaker.MarkSuccess();
                                tcs.SetResult(t.Result);
                                break;
                            case CTaskStatus.Faulted:
                                Status = CommandStatusEnum.Failed;
                                if (t.Exception.IsBadRequestException())
                                {
                                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.BadRequest);
                                    Log.Log(LogLevelEnum.Error, "HystrixCommand request is bad.", t.Exception, GetLogTagInfo().AddLogTagData("FXD303035"));
                                }
                                else
                                {
                                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Failed);
                                    Log.Log(LogLevelEnum.Error, "HystrixCommand execution failed.", t.Exception, GetLogTagInfo().AddLogTagData("FXD303036"));
                                }
                                needFallback = true;
                                break;
                            case CTaskStatus.Canceled:
                                Status = CommandStatusEnum.Timeout;
                                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Timeout);
                                Log.Log(LogLevelEnum.Warning, string.Format("timed out before executing run(), the wait time was {0} milliseconds; ", t.ExeuteMilliseconds)
                                    , GetLogTagInfo().AddLogTagData("FXD303034"));
                                needFallback = true;
                                break;
                            default:
                                break;
                        }


                        if (needFallback)
                        {
                            if (HasFallback)
                            {
                                try
                                {
                                    tcs.SetResult(ExecuteFallback());
                      
                                }
                                catch (Exception ex)
                                {
                                   throw(ex);

                                }

                            }
                            else if (e.Status == CTaskStatus.Faulted)
                            {
                                throw(t.Exception);
                            }
                            else if (e.Status == CTaskStatus.Canceled)
                            {
                                throw(new HystrixException(FailureTypeEnum.ExecutionTimeout, this.GetType(), Key,
                                    string.Format("timed out before executing run(), maxt waiting time was {0} milliseconds,the task waiting time was {1} milliseconds",
                                    ConfigSet.CommandTimeoutInMilliseconds, t.ExeuteMilliseconds),
                                    t.Exception, new Exception("no fallback found")));

                            }

                        }
                    }
                    catch(Exception ex)
                    {
                        Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);
                        tcs.TrySetException(ex);
                    }

                });

            }
            catch (Exception ex)
            {
                if (ex is HystrixException)
                {
                    if (((HystrixException)ex).FailureType == FailureTypeEnum.ThreadIsolationRejected)
                    {
                        Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Rejected);
                        Log.Log(LogLevelEnum.Error, "HystrixCommand execution rejected.", ex,
                            GetLogTagInfo().AddLogTagData("FXD303037"));
                        Status = CommandStatusEnum.Rejected;

                    }

                }

                if (HasFallback)
                {
                    tcs.Task.ContinueWith(t =>
                    {
                        try
                        {
                            var x = t.Exception;
                        }
                        catch
                        {
                        }
                    });

                    return GetFallBack();

                }

                Status = CommandStatusEnum.Failed;
                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);

                tcs.TrySetException(ex);
            }

            return tcs.Task;

        }

        private T ExecuteFallback()
        {
            if (FallbackExecutionSemaphore.TryAcquire())
            {
                try
                {
                    var rtn = ToIFallback().GetFallback();
                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.FallbackSuccess);
                    Status = CommandStatusEnum.FallbackSuccess;
                    Log.Log(LogLevelEnum.Warning, "HystrixCommand execution failed, use fallback instead.");
                    return rtn;
                }
                catch (Exception ex)
                {
                    Log.Log(LogLevelEnum.Error, "HystrixCommand fallback execution failed.", ex, 
                        GetLogTagInfo().AddLogTagData("FXD303038"));
                    Metrics.MarkExecutionEvent(CommandExecutionEventEnum.FallbackFailed);
                    Status = CommandStatusEnum.FallbackFailed;
                    throw ex;

                }
                finally
                {
                    FallbackExecutionSemaphore.Release();
                }
            }
            else
            {
                var fRejectMsg = "HystrixCommand fallback execution was rejected.";
                Log.Log(LogLevelEnum.Error, fRejectMsg, GetLogTagInfo().AddLogTagData("FXD303039"));
                Metrics.MarkExecutionEvent(CommandExecutionEventEnum.FallbackRejected);
                Status = CommandStatusEnum.FallbackRejected;
                throw new HystrixException(FailureTypeEnum.FallbackRejected,this.GetType(),Key,fRejectMsg);
            }

        }

        #endregion

    }
}
