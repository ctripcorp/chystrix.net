using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using CHystrix.CircuitBreaker;
using CHystrix.Config;
using CHystrix.Log;
using CHystrix.Metrics;

namespace CHystrix
{
    internal static class ComponentFactory
    {
        public const string CircuitBreakerRequestCountThresholdSettingKey = "CHystrix.CircuitBreakerRequestCountThreshold";
        public const string CircuitBreakerErrorThresholdPercentageSettingKey = "CHystrix.CircuitBreakerErrorThresholdPercentage";
        public const string CircuitBreakerForceClosedSettingKey = "CHystrix.CircuitBreakerForceClosed";
        public const string SemaphoreIsolationMaxConcurrentCountSettingKey = "CHystrix.SemaphoreIsolationMaxConcurrentCount";
        public const string ThreadIsolationMaxConcurrentCountSettingKey = "CHystrix.ThreadIsolationMaxConcurrentCount";
        public const string CommandTimeoutInMillisecondsSettingKey = "CHystrix.CommandTimeoutInMilliseconds";
        public const string MaxAsyncCommandExceedPercentageSettingKey = "CHystrix.MaxAsyncCommandExceedPercentage";
        public const string LogExecutionErrorSettingKey = "CHystrix.LogExecutionError";

        public const int FrameworkDefaultCircuitBreakerRequestCountThreshold = 20;
        public const int FrameworkDefaultCircuitBreakerErrorThresholdPercentage = 50;
        public const bool FrameworkDefaultCircuitBreakerForceClosed = false;
        public const int FrameworkDefaultSemaphoreIsolationMaxConcurrentCount = 100;
        public const int FrameworkDefaultThreadIsolationMaxConcurrentCount = 20;
        public const int FrameworkDefaultCommandTimeoutInMilliseconds = 30 * 1000;
        public const int FrameworkDefaultMaxAsyncCommandExceedPercentage = 50;
        public const bool FrameworkDefaultLogExecutionError = false;
        public const bool FrameworkDefaultDegradeLogLevel = false;

        public const int MinGlobalDefaultCircuitBreakerRequestCountThreshold = 10;
        public const int MinGlobalDefaultCircuitBreakerErrorThresholdPercentage = 20;
        public const int MinGlobalDefaultCommandMaxConcurrentCount = 50;
        public const int MinGlobalDefaultFallbackMaxConcurrentCount = 50;
        public const int MinGlobalDefaultCommandTimeoutInMilliseconds = 5000;

        public static int? GlobalDefaultCircuitBreakerRequestCountThreshold { get; set; }
        public static int? GlobalDefaultCircuitBreakerErrorThresholdPercentage { get; set; }
        public static bool? GlobalDefaultCircuitBreakerForceClosed { get; set; }
        public static int? GlobalDefaultCommandMaxConcurrentCount { get; set; }
        public static int? GlobalDefaultFallbackMaxConcurrentCount { get; set; }
        public static int? GlobalDefaultCommandTimeoutInMilliseconds { get; set; }

        public static readonly int? DefaultCircuitBreakerRequestCountThreshold;
        public static readonly int? DefaultCircuitBreakerErrorThresholdPercentage;
        public static readonly bool? DefaultCircuitBreakerForceClosed;
        public static readonly int? DefaultSemaphoreIsolationMaxConcurrentCount;
        public static readonly int? DefaultThreadIsolationMaxConcurrentCount;
        public static readonly int? DefaultCommandTimeoutInMilliseconds;
        public static readonly int DefaultMaxAsyncCommandExceedPercentage;
        public static readonly bool DefaultLogExecutionError;

        static ComponentFactory()
        {
            int defaultCircuitBreakerRequestCountThreshold;
            int.TryParse(ConfigurationManager.AppSettings[CircuitBreakerRequestCountThresholdSettingKey], out defaultCircuitBreakerRequestCountThreshold);
            if (defaultCircuitBreakerRequestCountThreshold > 0)
                DefaultCircuitBreakerRequestCountThreshold = defaultCircuitBreakerRequestCountThreshold;

            int defaultCircuitBreakerErrorThresholdPercentage;
            int.TryParse(ConfigurationManager.AppSettings[CircuitBreakerErrorThresholdPercentageSettingKey], out defaultCircuitBreakerErrorThresholdPercentage);
            if (defaultCircuitBreakerErrorThresholdPercentage > 0 && defaultCircuitBreakerErrorThresholdPercentage <= 100)
                DefaultCircuitBreakerErrorThresholdPercentage = defaultCircuitBreakerErrorThresholdPercentage;

            bool defaultCircuitBreakerForceClosed;
            if (bool.TryParse(ConfigurationManager.AppSettings[CircuitBreakerForceClosedSettingKey], out defaultCircuitBreakerForceClosed))
                DefaultCircuitBreakerForceClosed = defaultCircuitBreakerForceClosed;

            int defaultSemaphoreIsolationMaxConcurrentCount;
            int.TryParse(ConfigurationManager.AppSettings[SemaphoreIsolationMaxConcurrentCountSettingKey], out defaultSemaphoreIsolationMaxConcurrentCount);
            if (defaultSemaphoreIsolationMaxConcurrentCount > 0)
                DefaultSemaphoreIsolationMaxConcurrentCount = defaultSemaphoreIsolationMaxConcurrentCount;

            int defaultThreadIsolationMaxConcurrentCount;
            int.TryParse(ConfigurationManager.AppSettings[ThreadIsolationMaxConcurrentCountSettingKey], out defaultThreadIsolationMaxConcurrentCount);
            if (defaultThreadIsolationMaxConcurrentCount > 0)
                DefaultThreadIsolationMaxConcurrentCount = defaultThreadIsolationMaxConcurrentCount;

            int defaultCommandTimeoutInMilliseconds;
            int.TryParse(ConfigurationManager.AppSettings[CommandTimeoutInMillisecondsSettingKey], out defaultCommandTimeoutInMilliseconds);
            if (defaultCommandTimeoutInMilliseconds > 0)
                DefaultCommandTimeoutInMilliseconds = defaultCommandTimeoutInMilliseconds;

            int.TryParse(ConfigurationManager.AppSettings[MaxAsyncCommandExceedPercentageSettingKey], out DefaultMaxAsyncCommandExceedPercentage);
            if (DefaultMaxAsyncCommandExceedPercentage <= 0 || DefaultMaxAsyncCommandExceedPercentage > 100)
                DefaultMaxAsyncCommandExceedPercentage = FrameworkDefaultMaxAsyncCommandExceedPercentage;

            if (!bool.TryParse(ConfigurationManager.AppSettings[LogExecutionErrorSettingKey], out DefaultLogExecutionError))
                DefaultLogExecutionError = FrameworkDefaultLogExecutionError;
        }

        public static ICircuitBreaker CreateCircuitBreaker(ICommandConfigSet configSet, ICommandMetrics metrics)
        {
            return new CHystrix.CircuitBreaker.CircuitBreaker(configSet, metrics);
        }

        public static ICommandConfigSet CreateCommandConfigSet(IsolationModeEnum isolationMode)
        {
            CommandConfigSet configSet = new CommandConfigSet()
            {
                IsolationMode = isolationMode,
                CircuitBreakerEnabled = true,
                CircuitBreakerForceOpen = false,
                CircuitBreakerSleepWindowInMilliseconds = 5000,

                MetricsRollingStatisticalWindowBuckets = 10,
                MetricsRollingStatisticalWindowInMilliseconds = 10000,
                MetricsRollingPercentileEnabled = true,
                MetricsRollingPercentileWindowInMilliseconds = 60000,
                MetricsRollingPercentileWindowBuckets = 6,
                MetricsRollingPercentileBucketSize = 100,
                MetricsHealthSnapshotIntervalInMilliseconds = 100,

                MaxAsyncCommandExceedPercentage = DefaultMaxAsyncCommandExceedPercentage,
                DegradeLogLevel = FrameworkDefaultDegradeLogLevel,
                LogExecutionError = DefaultLogExecutionError
            };

            if (DefaultCircuitBreakerErrorThresholdPercentage.HasValue)
                configSet.CircuitBreakerErrorThresholdPercentage = DefaultCircuitBreakerErrorThresholdPercentage.Value;

            if (DefaultCircuitBreakerForceClosed.HasValue)
                configSet.CircuitBreakerForceClosed = DefaultCircuitBreakerForceClosed.Value;

            if (DefaultCircuitBreakerRequestCountThreshold.HasValue)
                configSet.CircuitBreakerRequestCountThreshold = DefaultCircuitBreakerRequestCountThreshold.Value;

            if (DefaultCommandTimeoutInMilliseconds.HasValue)
                configSet.CommandTimeoutInMilliseconds = DefaultCommandTimeoutInMilliseconds.Value;

            if (isolationMode == IsolationModeEnum.SemaphoreIsolation && DefaultSemaphoreIsolationMaxConcurrentCount.HasValue)
            {
                configSet.CommandMaxConcurrentCount = DefaultSemaphoreIsolationMaxConcurrentCount.Value;
                configSet.FallbackMaxConcurrentCount = configSet.CommandMaxConcurrentCount;
            }
            else if (isolationMode == IsolationModeEnum.ThreadIsolation && DefaultThreadIsolationMaxConcurrentCount.HasValue)
            {
                configSet.CommandMaxConcurrentCount = DefaultThreadIsolationMaxConcurrentCount.Value;
                configSet.FallbackMaxConcurrentCount = configSet.CommandMaxConcurrentCount;
            }

            return configSet;
        }

        public static ILog CreateLog(Type type)
        {
            return new NullLog(type);
        }

        public static ILog CreateLog(ICommandConfigSet configSet, Type type)
        {
            return new NullLog(configSet, type);
        }

        public static ICommandMetrics CreateCommandMetrics(ICommandConfigSet configSet, string key, IsolationModeEnum isolationMode)
        {
            if (isolationMode == IsolationModeEnum.SemaphoreIsolation)
                return new SemaphoreIsolationCommandMetrics(configSet, key);
            return new ThreadIsolationCommandMetrics(configSet, key);
        }
    }
}
