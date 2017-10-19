using System;
using System.Threading;
using CHystrix.CircuitBreaker;
using CHystrix;
using CHystrix.Utils.Atomic;
using CHystrix.Utils;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CHystrix.Metrics;
using CHystrix.Config;

namespace CHystrix.Test
{

    internal static class MetricsExe
    {
        public static void MarkSuccess(this ICommandMetrics metrics, int milliseconds=0)
        {
            metrics.MarkExecutionEvent(CommandExecutionEventEnum.Success);
        }

        public static void MarkFailure(this ICommandMetrics metrics, int milliseconds=0)
        {
            metrics.MarkExecutionEvent(CommandExecutionEventEnum.Failed);
        }

        public static void MarkTimeout(this ICommandMetrics metrics, int milliseconds=0)
        {
            metrics.MarkExecutionEvent(CommandExecutionEventEnum.Timeout);
        }

        public static double GetErrorPercentage(this ICommandMetrics metrics)
        {
            return metrics.GetExecutionHealthSnapshot().ErrorPercentage;
        }
    }

    [TestClass]
	public class HystrixCommandMetricsTest
    {

        #region Test Methods
        /// <summary>
		/// Testing the ErrorPercentage because this method could be easy to miss when making changes elsewhere.
		/// </summary>
        [TestMethod]
		public void TestGetErrorPercentage()
		{
            var config = GetCommandConfig();

            ICommandMetrics metrics = GetMetrics(config);

            metrics.MarkSuccess(1000);
            Thread.Sleep(config.MetricsHealthSnapshotIntervalInMilliseconds);
            Assert.AreEqual(0, metrics.GetErrorPercentage());

            metrics.MarkFailure();
            Thread.Sleep(config.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.AreEqual(50, metrics.GetErrorPercentage());

            metrics.MarkSuccess(100);
            metrics.MarkSuccess(100);
            Thread.Sleep(config.MetricsHealthSnapshotIntervalInMilliseconds);
            Assert.AreEqual(25, metrics.GetErrorPercentage());

            metrics.MarkTimeout(5000);
            metrics.MarkTimeout(5000);
            Thread.Sleep(config.MetricsHealthSnapshotIntervalInMilliseconds);
            Assert.AreEqual(50, metrics.GetErrorPercentage());

            metrics.MarkSuccess(100);
            metrics.MarkSuccess(100);
            metrics.MarkSuccess(100);

            // latent
            Thread.Sleep(config.MetricsHealthSnapshotIntervalInMilliseconds);
            metrics.MarkSuccess(5000);
            Thread.Sleep(config.MetricsHealthSnapshotIntervalInMilliseconds);

            // 6 success + 1 latent success + 1 failure + 2 timeout = 10 total
            // latent success not considered error
            // error percentage = 1 failure + 2 timeout / 10
            Assert.AreEqual(30, metrics.GetErrorPercentage());

		}
        #endregion

        #region private Method
        private ICommandMetrics GetMetrics(ICommandConfigSet configset)
        {
            return ComponentFactory.CreateCommandMetrics(configset, "BreakerTest",IsolationModeEnum.SemaphoreIsolation);

        }

        private ICommandConfigSet GetCommandConfig()
        {

            return new CommandConfigSet()
            {
                CircuitBreakerEnabled = true,
                CircuitBreakerRequestCountThreshold = 2,
                CircuitBreakerErrorThresholdPercentage = 50,
                CircuitBreakerForceClosed = false,
                CircuitBreakerForceOpen = false,
                CircuitBreakerSleepWindowInMilliseconds = 5000,

                MetricsRollingStatisticalWindowBuckets = 10,
                MetricsRollingStatisticalWindowInMilliseconds = 10000,
                MetricsRollingPercentileEnabled = true,
                MetricsRollingPercentileWindowInMilliseconds = 60000,
                MetricsRollingPercentileWindowBuckets = 6,
                MetricsRollingPercentileBucketSize = 100,
                MetricsHealthSnapshotIntervalInMilliseconds = 1,

                CommandMaxConcurrentCount = 10,
                CommandTimeoutInMilliseconds = 5000,

                FallbackMaxConcurrentCount = 10
            };
        }

        #endregion


	}

}