using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace CHystrix.Config
{
    [DataContract]
    internal class CommandConfigSet : ICommandConfigSet, IConfigChangeEvent
    {
        public IsolationModeEnum IsolationMode { get; set; }

        [DataMember]
        public bool CircuitBreakerEnabled { get; set; }

        private int? _circuitBreakerRequestCountThreshold;
        [DataMember]
        public int CircuitBreakerRequestCountThreshold
        {
            get
            {
                if (_circuitBreakerRequestCountThreshold.HasValue)
                    return _circuitBreakerRequestCountThreshold.Value;

                if (ComponentFactory.GlobalDefaultCircuitBreakerRequestCountThreshold.HasValue)
                    return ComponentFactory.GlobalDefaultCircuitBreakerRequestCountThreshold.Value;

                return ComponentFactory.FrameworkDefaultCircuitBreakerRequestCountThreshold;
            }
            set
            {
                if (value >= 0)
                    _circuitBreakerRequestCountThreshold = value;
            }
        }

        private int _circuitBreakerSleepWindowInMilliseconds;
        [DataMember]
        public int CircuitBreakerSleepWindowInMilliseconds
        {
            get { return _circuitBreakerSleepWindowInMilliseconds; }
            set
            {
                if (value > 0)
                    _circuitBreakerSleepWindowInMilliseconds = value;
            }
        }

        private int? _circuitBreakerErrorThresholdPercentage;
        [DataMember]
        public int CircuitBreakerErrorThresholdPercentage
        {
            get
            {
                if (_circuitBreakerErrorThresholdPercentage.HasValue)
                    return _circuitBreakerErrorThresholdPercentage.Value;

                if (ComponentFactory.GlobalDefaultCircuitBreakerErrorThresholdPercentage.HasValue)
                    return ComponentFactory.GlobalDefaultCircuitBreakerErrorThresholdPercentage.Value;

                return ComponentFactory.FrameworkDefaultCircuitBreakerErrorThresholdPercentage;
            }
            set
            {
                if (value > 0 && value <= 100)
                    _circuitBreakerErrorThresholdPercentage = value;
            }
        }

        [DataMember]
        public bool CircuitBreakerForceOpen { get; set; }

        private bool? _circuitBreakerForceClosed;
        [DataMember]
        public bool CircuitBreakerForceClosed
        {
            get
            {
                if (_circuitBreakerForceClosed.HasValue)
                    return _circuitBreakerForceClosed.Value;

                if (ComponentFactory.GlobalDefaultCircuitBreakerForceClosed.HasValue)
                    return ComponentFactory.GlobalDefaultCircuitBreakerForceClosed.Value;

                return ComponentFactory.FrameworkDefaultCircuitBreakerForceClosed;
            }
            set
            {
                _circuitBreakerForceClosed = value;
            }
        }

        private int _metricsRollingStatisticalWindowInMilliseconds;
        [DataMember]
        public int MetricsRollingStatisticalWindowInMilliseconds
        {
            get { return _metricsRollingStatisticalWindowInMilliseconds; }
            set
            {
                if (value > 0)
                    _metricsRollingStatisticalWindowInMilliseconds = value;
            }
        }

        private int _metricsRollingStatisticalWindowBuckets;
        [DataMember]
        public int MetricsRollingStatisticalWindowBuckets
        {
            get { return _metricsRollingStatisticalWindowBuckets; }
            set
            {
                if (value > 0)
                    _metricsRollingStatisticalWindowBuckets = value;
            }
        }

        [DataMember]
        public bool MetricsRollingPercentileEnabled { get; set; }

        private int _metricsRollingPercentileWindowInMilliseconds;
        [DataMember]
        public int MetricsRollingPercentileWindowInMilliseconds
        {
            get { return _metricsRollingPercentileWindowInMilliseconds; }
            set
            {
                if (value > 0)
                    _metricsRollingPercentileWindowInMilliseconds = value;
            }
        }

        private int _metricsRollingPercentileWindowBuckets;
        [DataMember]
        public int MetricsRollingPercentileWindowBuckets
        {
            get { return _metricsRollingPercentileWindowBuckets; }
            set
            {
                if (value > 0)
                    _metricsRollingPercentileWindowBuckets = value;
            }
        }

        private int _metricsRollingPercentileBucketSize;
        [DataMember]
        public int MetricsRollingPercentileBucketSize
        {
            get { return _metricsRollingPercentileBucketSize; }
            set
            {
                if (value > 0)
                    _metricsRollingPercentileBucketSize = value;
            }
        }

        private int _metricsHealthSnapshotIntervalInMilliseconds;
        [DataMember]
        public int MetricsHealthSnapshotIntervalInMilliseconds
        {
            get { return _metricsHealthSnapshotIntervalInMilliseconds; }
            set
            {
                if (value > 0)
                    _metricsHealthSnapshotIntervalInMilliseconds = value;
            }
        }

        private int? _commandTimeoutInMilliseconds;
        [DataMember]
        public int CommandTimeoutInMilliseconds
        {
            get
            {
                if (_commandTimeoutInMilliseconds.HasValue)
                    return _commandTimeoutInMilliseconds.Value;

                if (ComponentFactory.GlobalDefaultCommandTimeoutInMilliseconds.HasValue)
                    return ComponentFactory.GlobalDefaultCommandTimeoutInMilliseconds.Value;

                return ComponentFactory.FrameworkDefaultCommandTimeoutInMilliseconds;
            }
            set
            {
                if (value > 0)
                    _commandTimeoutInMilliseconds = value;
            }
        }

        private int? _commandMaxConcurrentCount;
        [DataMember]
        public int CommandMaxConcurrentCount
        {
            get
            {
                if (_commandMaxConcurrentCount.HasValue)
                    return _commandMaxConcurrentCount.Value;

                if (IsolationMode == IsolationModeEnum.ThreadIsolation)
                    return ComponentFactory.FrameworkDefaultThreadIsolationMaxConcurrentCount;

                if (ComponentFactory.GlobalDefaultCommandMaxConcurrentCount.HasValue)
                    return ComponentFactory.GlobalDefaultCommandMaxConcurrentCount.Value;

                return ComponentFactory.FrameworkDefaultSemaphoreIsolationMaxConcurrentCount;
            }
            set
            {
                if (value > 0)
                    _commandMaxConcurrentCount = value;
            }
        }

        private int? _fallbackMaxConcurrentCount;
        [DataMember]
        public int FallbackMaxConcurrentCount
        {
            get
            {
                if (_fallbackMaxConcurrentCount.HasValue)
                    return _fallbackMaxConcurrentCount.Value;

                if (IsolationMode == IsolationModeEnum.ThreadIsolation)
                    return ComponentFactory.FrameworkDefaultThreadIsolationMaxConcurrentCount;

                if (ComponentFactory.GlobalDefaultFallbackMaxConcurrentCount.HasValue)
                    return ComponentFactory.GlobalDefaultFallbackMaxConcurrentCount.Value;

                return ComponentFactory.FrameworkDefaultSemaphoreIsolationMaxConcurrentCount;
            }
            set
            {
                if (value > 0)
                    _fallbackMaxConcurrentCount = value;
            }
        }

        [DataMember]
        public bool DegradeLogLevel { get; set; }

        [DataMember]
        public bool LogExecutionError { get; set; }

        private int _maxAsyncCommandExceedPercentage;
        [DataMember]
        public int MaxAsyncCommandExceedPercentage
        {
            get { return _maxAsyncCommandExceedPercentage; }
            set
            {
                if (value >= 0 && value <= 100)
                    _maxAsyncCommandExceedPercentage = value;
            }
        }

        private readonly object EventLock = new object();
        private event HandleConfigChangeDelegate onConfigChanged;

        public CommandConfigSet()
        {
        }

        event HandleConfigChangeDelegate IConfigChangeEvent.OnConfigChanged
        {
            add
            {
                lock (EventLock)
                {
                    onConfigChanged += value;
                }
            }
            remove
            {
                lock (EventLock)
                {
                    onConfigChanged -= value;
                }
            }
        }

        void IConfigChangeEvent.RaiseConfigChangeEvent()
        {
            if (onConfigChanged != null)
                onConfigChanged(this);
        }
    }
}
