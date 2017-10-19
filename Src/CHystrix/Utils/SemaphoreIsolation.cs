using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

using CHystrix.Metrics;

namespace CHystrix.Utils
{
    public class SemaphoreIsolation : IDisposable
    {
        #region Config

        public static void Config(string commandKey, Action<ICommandConfigSet> config)
        {
            Config(commandKey, null, config);
        }

        public static void Config(string commandKey, string groupKey, Action<ICommandConfigSet> config)
        {
            Config(commandKey, groupKey, null, config);
        }

        public static void Config(string commandKey, string groupKey)
        {
            Config(commandKey, groupKey, domain: null);
        }

        public static void Config(string commandKey, string groupKey, string domain)
        {
            Config(null, commandKey, groupKey, domain);
        }

        public static void Config(string instanceKey, string commandKey, string groupKey, string domain)
        {
            Config(instanceKey, commandKey, groupKey, domain, null);
        }

        public static void Config(string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            Config(null, commandKey, groupKey, domain, maxConcurrentCount);
        }

        public static void Config(string instanceKey, string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            Config(instanceKey, commandKey, groupKey, domain, maxConcurrentCount, fallbackMaxConcurrentCount: maxConcurrentCount);
        }

        public static void Config(string commandKey, string groupKey, string domain, int maxConcurrentCount, int timeoutInMilliseconds)
        {
            Config(null, commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds);
        }

        public static void Config(string instanceKey, string commandKey, string groupKey, string domain, int maxConcurrentCount, int timeoutInMilliseconds)
        {
            Config(instanceKey, commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds, fallbackMaxConcurrentCount: maxConcurrentCount);
        }

        internal static void Config(string commandKey, string groupKey, string domain, int? maxConcurrentCount = null,
            int? timeoutInMilliseconds = null, int? circuitBreakerRequestCountThreshold = null, int? circuitBreakerErrorThresholdPercentage = null,
            int? fallbackMaxConcurrentCount = null)
        {
            Config(null, commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds, circuitBreakerRequestCountThreshold,
                circuitBreakerErrorThresholdPercentage, fallbackMaxConcurrentCount);
        }

        internal static void Config(string instanceKey, string commandKey, string groupKey, string domain, int? maxConcurrentCount = null,
            int? timeoutInMilliseconds = null, int? circuitBreakerRequestCountThreshold = null, int? circuitBreakerErrorThresholdPercentage = null,
            int? fallbackMaxConcurrentCount = null)
        {
            Config(instanceKey, commandKey, groupKey, domain,
                configSet =>
                {
                    if (maxConcurrentCount.HasValue)
                        configSet.CommandMaxConcurrentCount = maxConcurrentCount.Value;
                    if (timeoutInMilliseconds.HasValue)
                        configSet.CommandTimeoutInMilliseconds = timeoutInMilliseconds.Value;
                    if (circuitBreakerRequestCountThreshold.HasValue)
                        configSet.CircuitBreakerRequestCountThreshold = circuitBreakerRequestCountThreshold.Value;
                    if (circuitBreakerErrorThresholdPercentage.HasValue)
                        configSet.CircuitBreakerErrorThresholdPercentage = circuitBreakerErrorThresholdPercentage.Value;
                    if (fallbackMaxConcurrentCount.HasValue)
                        configSet.FallbackMaxConcurrentCount = fallbackMaxConcurrentCount.Value;
                });
        }

        public static void Config(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
        {
            Config(null, commandKey, groupKey, domain, config);
        }

        public static void Config(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
        {
            new SemaphoreIsolation(instanceKey, commandKey, groupKey, domain, config);
        }

        #endregion

        #region Isolation

        public static object CreateInstance(string commandKey)
        {
            return new SemaphoreIsolation(commandKey);
        }

        public static object CreateInstance(string instanceKey, string commandKey)
        {
            return new SemaphoreIsolation(instanceKey, commandKey, null, null, null);
        }

        /// <summary>
        /// HystrixException will be thrown when execution is short-circuited or rejected.
        /// Once an instance is started, a semaphore is occupied. 
        /// It's required to call EndExecution(instance) to release the semaphore after execution.
        /// </summary>
        /// <exception cref="HystrixException"></exception>
        public static void StartExecution(object instance)
        {
            SemaphoreIsolation isolationInstance = ConvertInstance(instance);
            isolationInstance.StartExecution();
        }

        public static void MarkSuccess(object instance)
        {
            SemaphoreIsolation isolationInstance = ConvertInstance(instance);
            isolationInstance.MarkSuccess();
        }

        public static void MarkFailure(object instance)
        {
            SemaphoreIsolation isolationInstance = ConvertInstance(instance);
            isolationInstance.MarkFailure();
        }

        public static void MarkBadRequest(object instance)
        {
            SemaphoreIsolation isolationInstance = ConvertInstance(instance);
            isolationInstance.MarkBadRequest();
        }

        /// <summary>
        /// Release the occupied semaphore
        /// </summary>
        public static void EndExecution(object instance)
        {
            SemaphoreIsolation isolationInstance = ConvertInstance(instance);
            isolationInstance.EndExecution();
        }

        private static SemaphoreIsolation ConvertInstance(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            SemaphoreIsolation isolationInstance = instance as SemaphoreIsolation;
            if (isolationInstance == null)
                throw new ArgumentException("instance should be of type SemaphoreIsolation!");

            return isolationInstance;
        }

        #endregion

        readonly string Key;
        readonly CommandComponents Components;
        readonly IsolationSemaphore ExecutionSemaphore;

        bool _hasSemaphore;
        Stopwatch _stopwatch;
        int _started;
        int _markedResult;
        int _disposed;

        public SemaphoreIsolation(string commandKey)
            : this(commandKey, null)
        {
        }

        public SemaphoreIsolation(string commandKey, string groupKey)
            : this(commandKey, groupKey, null)
        {
        }

        public SemaphoreIsolation(string commandKey, string groupKey, string domain)
            : this(commandKey, groupKey, domain, null)
        {
        }

        public SemaphoreIsolation(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
            : this(null, commandKey, groupKey, domain, config)
        {
        }

        public SemaphoreIsolation(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                string message = "HystrixCommand Key cannot be null.";
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303002"));
                throw new ArgumentNullException(message);
            }

            Key = CommonUtils.GenerateKey(instanceKey, commandKey);
            instanceKey = string.IsNullOrWhiteSpace(instanceKey) ? null : instanceKey.Trim();
            commandKey = commandKey.Trim();
            groupKey = groupKey ?? HystrixCommandBase.DefaultGroupKey;
            domain = domain ?? CommandDomains.Default;

            Components = HystrixCommandBase.CommandComponentsCollection.GetOrAdd(Key, key =>
                HystrixCommandBase.CreateCommandComponents(Key, instanceKey,
                    commandKey, groupKey, domain, IsolationModeEnum.SemaphoreIsolation, config, typeof(SemaphoreIsolation)));
            ExecutionSemaphore = HystrixCommandBase.ExecutionSemaphores.GetOrAdd(Key,
                key => new IsolationSemaphore(Components.ConfigSet.CommandMaxConcurrentCount));

            _stopwatch = new Stopwatch();
        }

        /// <summary>
        /// HystrixException will be thrown when execution is short-circuited or rejected.
        /// Once an instance is started, a semaphore is occupied. 
        /// It's required to call EndExecution(instance) to release the semaphore after execution.
        /// </summary>
        /// <exception cref="HystrixException"></exception>
        public void StartExecution()
        {
            if (!CanStartExecution())
                return;

            _stopwatch.Start();

            if (!Components.CircuitBreaker.AllowRequest())
            {
                Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ShortCircuited);
                Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);
                Interlocked.CompareExchange(ref _markedResult, 1, 0);

                string errorMessage = "Circuit Breaker is open. Execution was short circuited.";
                Components.Log.Log(LogLevelEnum.Error, errorMessage, GetLogTagInfo().AddLogTagData("FXD303019"));
                throw new HystrixException(FailureTypeEnum.ShortCircuited, this.GetType(), Key, errorMessage);
            }

            _hasSemaphore = ExecutionSemaphore.TryAcquire();
            if (!_hasSemaphore)
            {
                Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Rejected);
                Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);
                Interlocked.CompareExchange(ref _markedResult, 1, 0);

                string errorMessage = "HystrixCommand execution was rejected.";
                Components.Log.Log(LogLevelEnum.Error, errorMessage, GetLogTagInfo().AddLogTagData("FXD303023"));
                throw new HystrixException(FailureTypeEnum.SemaphoreIsolationRejected, GetType(), Key, errorMessage);
            }
        }

        public void MarkSuccess()
        {
            if (!CanMarkResult())
                return;

            bool isTimeout = _stopwatch.ElapsedMilliseconds > Components.ConfigSet.CommandTimeoutInMilliseconds;
            if (isTimeout)
            {
                Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Timeout);
                Components.Log.Log(LogLevelEnum.Warning, string.Format("HystrixCommand execution timeout: {0}ms.", _stopwatch.ElapsedMilliseconds),
                    GetLogTagInfo().AddLogTagData("FXD303020"));
                return;
            }

            Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Success);
            Components.CircuitBreaker.MarkSuccess();
        }

        public void MarkFailure()
        {
            if (!CanMarkResult())
                return;

            Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.Failed);
            Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);

            if (Components.ConfigSet.LogExecutionError)
                Components.Log.Log(LogLevelEnum.Error, "HystrixCommand execution failed.", GetLogTagInfo().AddLogTagData("FXD303022"));
        }

        public void MarkBadRequest()
        {
            if (!CanMarkResult())
                return;

            Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.BadRequest);
            Components.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ExceptionThrown);

            if (Components.ConfigSet.LogExecutionError)
                Components.Log.Log(LogLevelEnum.Error, "HystrixCommand request is bad.", GetLogTagInfo().AddLogTagData("FXD303021"));
        }

        /// <summary>
        /// Release the occupied semaphore
        /// </summary>
        public void EndExecution()
        {
            if (!CanEndExecution())
                return;

            if (!_hasSemaphore)
                return;

            ExecutionSemaphore.Release();
            Components.Metrics.MarkExecutionLatency(_stopwatch.ElapsedMilliseconds);
            Components.Metrics.MarkTotalExecutionLatency(_stopwatch.ElapsedMilliseconds);
        }

        private Dictionary<string, string> GetLogTagInfo()
        {
            return new Dictionary<string, string>()
            {
                { "HystrixAppName", HystrixCommandBase.HystrixAppName },
                { "Key", Key },
                { "InstanceKey", Components.CommandInfo.InstanceKey },
                { "CommandKey", Components.CommandInfo.CommandKey },
                { "GroupKey", Components.CommandInfo.GroupKey },
                { "Domain", Components.CommandInfo.Domain },
                { "IsolationMode", Components.CommandInfo.Type }
            };
        }

        private bool CanStartExecution()
        {
            bool canStart = _started == 0 && Interlocked.CompareExchange(ref _started, 1, 0) == 0;
            if (canStart)
                return true;

            Components.Log.Log(LogLevelEnum.Warning, "The command has been started. A command can be only started once.",
                GetLogTagInfo().AddLogTagData("FXD303018"));
            return false;
        }

        private bool CanMarkResult()
        {
            if (_started == 0)
            {
                Components.Log.Log(LogLevelEnum.Warning, "The command has not been started.",
                    GetLogTagInfo().AddLogTagData("FXD303040"));
                return false;
            }

            bool canMarkResult = _markedResult == 0 && Interlocked.CompareExchange(ref _markedResult, 1, 0) == 0;
            if (!canMarkResult)
                return false;

            if (_stopwatch.IsRunning)
                _stopwatch.Stop();

            return true;
        }

        private bool CanEndExecution()
        {
            if (_started == 0)
            {
                Components.Log.Log(LogLevelEnum.Warning, "The command has not been started.",
                    GetLogTagInfo().AddLogTagData("FXD303041"));
                return false;
            }

            bool canEnd = _disposed == 0 && Interlocked.CompareExchange(ref _disposed, 1, 0) == 0;
            if (!canEnd)
            {
                Components.Log.Log(LogLevelEnum.Warning, "The command has been ended. A command can be only ended once.",
                    GetLogTagInfo().AddLogTagData("FXD303042"));
                return false;
            }

            if (_stopwatch.IsRunning)
                _stopwatch.Stop();

            return true;
        }

        /// <summary>
        /// Release the occupied semaphore
        /// Alias to the EndExecution() method
        /// </summary>
        public void Dispose()
        {
            EndExecution();
        }
    }
}
