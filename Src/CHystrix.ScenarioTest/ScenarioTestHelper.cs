using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CHystrix;
using CHystrix.Utils;
using CHystrix.Config;
using CircuitBreakerImpl = CHystrix.CircuitBreaker.CircuitBreaker;
using CHystrix.Metrics;

namespace CHystrix.ScenarioTest
{
    internal static class ScenarioTestHelper
    {
        public const int HealthSnapshotIntervalInMilliseconds = 1;
        public static Random Random = new Random((int)(CommonUtils.CurrentTimeInMiliseconds % (1 * 1000 * 1000)));

        public static CommandConfigSet ToConcrete(this ICommandConfigSet configSet)
        {
            return (CommandConfigSet)configSet;
        }

        public static CircuitBreakerImpl ToConcrete(this ICircuitBreaker circuitBreaker)
        {
            return (CircuitBreakerImpl)circuitBreaker;
        }
 
        public static CommandMetrics ToConcrete(this ICommandMetrics metrics)
        {
            return (CommandMetrics)metrics;
        }

        public static void InitTestHealthSnapshotInterval(this ICommandConfigSet configSet)
        {
            configSet.ToConcrete().MetricsHealthSnapshotIntervalInMilliseconds = HealthSnapshotIntervalInMilliseconds;
        }

        public static void SleepHealthSnapshotInverval()
        {
            Thread.Sleep(HealthSnapshotIntervalInMilliseconds);
        }

        public static bool AreEqual(ICommandConfigSet configSet1, ICommandConfigSet configSet2)
        {
            if (configSet1 == configSet2)
                return true;

            if (configSet1 == null || configSet2 == null)
                return false;

            return configSet1.CircuitBreakerEnabled == configSet2.CircuitBreakerEnabled
                && configSet1.CircuitBreakerErrorThresholdPercentage == configSet2.CircuitBreakerErrorThresholdPercentage
                && configSet1.CircuitBreakerForceClosed == configSet2.CircuitBreakerForceClosed
                && configSet1.CircuitBreakerForceOpen == configSet2.CircuitBreakerForceOpen
                && configSet1.CircuitBreakerRequestCountThreshold == configSet2.CircuitBreakerRequestCountThreshold
                && configSet1.CircuitBreakerSleepWindowInMilliseconds == configSet2.CircuitBreakerSleepWindowInMilliseconds
                && configSet1.CommandMaxConcurrentCount == configSet2.CommandMaxConcurrentCount
                && configSet1.CommandTimeoutInMilliseconds == configSet2.CommandTimeoutInMilliseconds
                && configSet1.DegradeLogLevel == configSet2.DegradeLogLevel
                && configSet1.FallbackMaxConcurrentCount == configSet2.FallbackMaxConcurrentCount
                && configSet1.LogExecutionError == configSet2.LogExecutionError
                && configSet1.MaxAsyncCommandExceedPercentage == configSet2.MaxAsyncCommandExceedPercentage
                && configSet1.MetricsHealthSnapshotIntervalInMilliseconds == configSet2.MetricsHealthSnapshotIntervalInMilliseconds
                && configSet1.MetricsRollingPercentileBucketSize == configSet2.MetricsRollingPercentileBucketSize
                && configSet1.MetricsRollingPercentileEnabled == configSet2.MetricsRollingPercentileEnabled
                && configSet1.MetricsRollingPercentileWindowBuckets == configSet2.MetricsRollingPercentileWindowBuckets
                && configSet1.MetricsRollingPercentileWindowInMilliseconds == configSet2.MetricsRollingPercentileWindowInMilliseconds
                && configSet1.MetricsRollingStatisticalWindowBuckets == configSet2.MetricsRollingStatisticalWindowBuckets
                && configSet1.MetricsRollingStatisticalWindowInMilliseconds == configSet2.MetricsRollingStatisticalWindowInMilliseconds;
        }

        public static void SetCommandConfigFrom(ICommandConfigSet configSet1, ICommandConfigSet configSet2)
        {
            configSet1.ToConcrete().CircuitBreakerEnabled = configSet2.ToConcrete().CircuitBreakerEnabled;
            configSet1.CircuitBreakerErrorThresholdPercentage = configSet2.CircuitBreakerErrorThresholdPercentage;
            configSet1.CircuitBreakerForceClosed = configSet2.CircuitBreakerForceClosed;
            configSet1.CircuitBreakerForceOpen = configSet2.CircuitBreakerForceOpen;
            configSet1.CircuitBreakerRequestCountThreshold = configSet2.CircuitBreakerRequestCountThreshold;
            configSet1.ToConcrete().CircuitBreakerSleepWindowInMilliseconds = configSet2.CircuitBreakerSleepWindowInMilliseconds;
            configSet1.CommandMaxConcurrentCount = configSet2.CommandMaxConcurrentCount;
            configSet1.CommandTimeoutInMilliseconds = configSet2.CommandTimeoutInMilliseconds;
            configSet1.DegradeLogLevel = configSet2.DegradeLogLevel;
            configSet1.FallbackMaxConcurrentCount = configSet2.FallbackMaxConcurrentCount;
            configSet1.LogExecutionError = configSet2.LogExecutionError;
            configSet1.MaxAsyncCommandExceedPercentage = configSet2.MaxAsyncCommandExceedPercentage;
            configSet1.ToConcrete().MetricsHealthSnapshotIntervalInMilliseconds = configSet2.MetricsHealthSnapshotIntervalInMilliseconds;
            configSet1.ToConcrete().MetricsRollingPercentileBucketSize = configSet2.MetricsRollingPercentileBucketSize;
            configSet1.ToConcrete().MetricsRollingPercentileEnabled = configSet2.MetricsRollingPercentileEnabled;
            configSet1.ToConcrete().MetricsRollingPercentileWindowBuckets = configSet2.MetricsRollingPercentileWindowBuckets;
            configSet1.ToConcrete().MetricsRollingPercentileWindowInMilliseconds = configSet2.MetricsRollingPercentileWindowInMilliseconds;
            configSet1.ToConcrete().MetricsRollingStatisticalWindowBuckets = configSet2.MetricsRollingStatisticalWindowBuckets;
            configSet1.ToConcrete().MetricsRollingStatisticalWindowInMilliseconds = configSet2.MetricsRollingStatisticalWindowInMilliseconds;
        }

        public static ICommandConfigSet CreateCustomConfigSet(IsolationModeEnum isolationMode = IsolationModeEnum.SemaphoreIsolation)
        {
            ICommandConfigSet configSet = ComponentFactory.CreateCommandConfigSet(isolationMode);

            configSet.ToConcrete().CircuitBreakerEnabled = false;
            configSet.CircuitBreakerErrorThresholdPercentage = 99;
            configSet.CircuitBreakerForceClosed = true;
            configSet.CircuitBreakerForceOpen = true;
            configSet.CircuitBreakerRequestCountThreshold = 99;
            configSet.ToConcrete().CircuitBreakerSleepWindowInMilliseconds = 4999;
            configSet.CommandMaxConcurrentCount = 99;
            configSet.CommandTimeoutInMilliseconds = 4999;
            configSet.DegradeLogLevel = true;
            configSet.FallbackMaxConcurrentCount = 4999;
            configSet.LogExecutionError = true;
            configSet.MaxAsyncCommandExceedPercentage = 99;
            configSet.ToConcrete().MetricsHealthSnapshotIntervalInMilliseconds = 10;
            configSet.ToConcrete().MetricsRollingPercentileBucketSize = 50;
            configSet.ToConcrete().MetricsRollingPercentileEnabled = false;
            configSet.ToConcrete().MetricsRollingPercentileWindowBuckets = 20;
            configSet.ToConcrete().MetricsRollingPercentileWindowInMilliseconds = 80 * 1000;
            configSet.ToConcrete().MetricsRollingStatisticalWindowBuckets = 20;
            configSet.ToConcrete().MetricsRollingStatisticalWindowInMilliseconds = 200 * 1000;

            return configSet;
        }
    }
}
