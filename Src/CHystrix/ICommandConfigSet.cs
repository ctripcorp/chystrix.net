using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    public interface ICommandConfigSet
    {
        /// <summary>
        /// Whether circuit breaker should be enabled.
        /// </summary>
        bool CircuitBreakerEnabled { get; }

        /// <summary>
        /// number of requests that must be made within a statisticalWindow before open/close decisions are made using stats
        /// </summary>
        int CircuitBreakerRequestCountThreshold { get; set; }

        /// <summary>
        /// milliseconds after tripping circuit before allowing retry
        /// </summary>
        int CircuitBreakerSleepWindowInMilliseconds { get; }

        /// <summary>
        /// % of 'marks' that must be failed to trip the circuit
        /// </summary>
        int CircuitBreakerErrorThresholdPercentage { get; set; }

        /// <summary>
        /// a property to allow forcing the circuit open (stopping all requests)
        /// </summary>
        bool CircuitBreakerForceOpen { get; set; }

        /// <summary>
        /// a property to allow ignoring errors and therefore never trip 'open' (ie. allow all traffic through)
        /// </summary>
        bool CircuitBreakerForceClosed { get; set; }

        /// <summary>
        /// milliseconds back that will be tracked
        /// </summary>
        int MetricsRollingStatisticalWindowInMilliseconds { get; }

        /// <summary>
        /// number of buckets in the statisticalWindow
        /// </summary>
        int MetricsRollingStatisticalWindowBuckets { get; }

        /// <summary>
        /// Whether monitoring should be enabled (SLA and Tracers).
        /// </summary>
        bool MetricsRollingPercentileEnabled { get; }

        /// <summary>
        /// number of milliseconds that will be tracked in RollingPercentile
        /// </summary>
        int MetricsRollingPercentileWindowInMilliseconds { get; }

        /// <summary>
        /// number of buckets percentileWindow will be divided into
        /// </summary>
        int MetricsRollingPercentileWindowBuckets { get; }

        /// <summary>
        /// how many values will be stored in each percentileWindowBucket
        /// </summary>
        int MetricsRollingPercentileBucketSize { get; }

        /// <summary>
        /// time between health snapshots
        /// </summary>
        int MetricsHealthSnapshotIntervalInMilliseconds { get; }

        /// <summary>
        /// If command execution time exceeds timeout, mark the execution timeout no matter it will pass or not
        /// In Thread Isolation, if an execution has not been started but elapsed time has exceeded timeout, execution will be cancelled
        /// </summary>
        int CommandTimeoutInMilliseconds { get; set; }

        /// <summary>
        /// For Semaphore Isolation, it is the max semaphore count
        /// For Thread Isolation, it is the max thread count the command can use (thread pool size)
        /// </summary>
        int CommandMaxConcurrentCount { get; set; }

        /// <summary>
        /// Max semaphore count for fallback execution
        /// </summary>
        int FallbackMaxConcurrentCount { get; set; }

        /// <summary>
        /// Let fatal as error, error as warning, warning as info
        /// </summary>
        bool DegradeLogLevel { get; set; }

        /// <summary>
        /// Whether to log execution error. Can disable it when execution self has logged error. Default to true
        /// </summary>
        bool LogExecutionError { get; set; }

        /// <summary>
        /// when async command's count exceed thread pool's volume, only allow some percentage (reference pool volume) task queue.
        /// </summary>
        int MaxAsyncCommandExceedPercentage { get; set; }
    }
}
