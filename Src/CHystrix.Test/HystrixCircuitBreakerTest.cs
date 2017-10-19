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
    #region TestCircuitBreaker
    internal class TestCircuitBreaker : ICircuitBreaker
    {
         readonly ICommandConfigSet ConfigSet;
         readonly ICommandMetrics _Metrics;

         readonly AtomicBoolean OpenFlag;
         readonly AtomicLong CircuitOpenedOrLastTestedTime;


         public TestCircuitBreaker()
         {
             ConfigSet = new CommandConfigSet()
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

             _Metrics =  ComponentFactory.CreateCommandMetrics(ConfigSet, "BreakerTest",IsolationModeEnum.SemaphoreIsolation);

             OpenFlag = new AtomicBoolean();
             CircuitOpenedOrLastTestedTime = new AtomicLong();


         }


        public TestCircuitBreaker(ICommandConfigSet configSet, ICommandMetrics metrics)
        {
            ConfigSet = configSet;
            _Metrics = metrics;

            OpenFlag = new AtomicBoolean();
            CircuitOpenedOrLastTestedTime = new AtomicLong();
        }

        public bool AllowRequest()
        {
            if (!ConfigSet.CircuitBreakerEnabled)
                return true;

            if (ConfigSet.CircuitBreakerForceOpen)
                return false;

            if (ConfigSet.CircuitBreakerForceClosed)
            {
                // we still want to allow IsOpen() to perform it's calculations so we simulate normal behavior
                IsOpen();

                return true;
            }

            return !this.IsOpen() || this.AllowSingleTest();
        }

        public bool IsOpen()
        {
            if (!ConfigSet.CircuitBreakerEnabled)
                return false;

            if (ConfigSet.CircuitBreakerForceOpen)
                return true;

            CommandExecutionHealthSnapshot healthSnapshot = _Metrics.GetExecutionHealthSnapshot();

            if (healthSnapshot.TotalCount < ConfigSet.CircuitBreakerRequestCountThreshold)
                return false;

            if (healthSnapshot.ErrorPercentage < ConfigSet.CircuitBreakerErrorThresholdPercentage)
                return false;

            // our failure rate is too high, trip the circuit
            if (this.OpenFlag.CompareAndSet(false, true))
            {
                // if the previousValue was false then we want to set the currentTime
                // How could previousValue be true? If another thread was going through this code at the same time a race-condition could have
                // caused another thread to set it to true already even though we were in the process of doing the same
                this.CircuitOpenedOrLastTestedTime.Value = CommonUtils.CurrentTimeInMiliseconds;
                CommonUtils.Log.Log(
                    LogLevelEnum.Fatal,
                    "Circuit Breaker is open after lots of fail or timeout happen.",
                    new Dictionary<string, string>()
                    {
                        { "CircuitBreaker", "Open" }
                    });
            }

            return true;
        }

        public ICommandMetrics Metrics
        {

            get
            {
                return this._Metrics;
            }
        }

        public void MarkSuccess()
        {
            if (!ConfigSet.CircuitBreakerEnabled)
                return;

            if (OpenFlag)
            {
                OpenFlag.Value = false;
                _Metrics.Reset();
                CommonUtils.Log.Log(
                    LogLevelEnum.Info,
                    "Circuit Breaker is closed after a command execution succeeded.",
                    new Dictionary<string, string>()
                    {
                        { "CircuitBreaker", "Closed" }
                    });
            }
        }

        /// <summary>
        /// Gets whether the circuit breaker should permit a single test request.
        /// </summary>
        /// <returns>True if single test is permitted, otherwise false.</returns>
        private bool AllowSingleTest()
        {
            long timeCircuitOpenedOrWasLastTested = CircuitOpenedOrLastTestedTime.Value;

            // 1) if the circuit is open
            // 2) and it's been longer than 'sleepWindow' since we opened the circuit
            if (OpenFlag && CommonUtils.CurrentTimeInMiliseconds > timeCircuitOpenedOrWasLastTested + ConfigSet.CircuitBreakerSleepWindowInMilliseconds)
            {
                // We push the 'circuitOpenedTime' ahead by 'sleepWindow' since we have allowed one request to try.
                // If it succeeds the circuit will be closed, otherwise another singleTest will be allowed at the end of the 'sleepWindow'.
                if (CircuitOpenedOrLastTestedTime.CompareAndSet(timeCircuitOpenedOrWasLastTested, CommonUtils.CurrentTimeInMiliseconds))
                {
                    // if this returns true that means we set the time so we'll return true to allow the singleTest
                    // if it returned false it means another thread raced us and allowed the singleTest before we did
                    return true;
                }
            }

            return false;
        }

    }
    #endregion

    [TestClass]
    public class HystrixCircuitBreakerTest
    {

        #region Test Methods
        /// <summary>
        /// Test that if all 'marks' are successes during the test window that it does NOT trip the circuit.
        /// Test that if all 'marks' are failures during the test window that it trips the circuit.
        /// </summary>
        [TestMethod]
        public virtual void TestTripCircuit()
        {

            var properties = GetCommandConfig();
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            metrics.MarkSuccess();
            metrics.MarkSuccess();
            metrics.MarkSuccess();


            // this should still allow requests as everything has been successful
            Thread.Sleep(properties.MetricsHealthSnapshotIntervalInMilliseconds);
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsFalse(cb.IsOpen());

            // fail
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure(); ;
            Thread.Sleep(properties.MetricsHealthSnapshotIntervalInMilliseconds);

            // everything has failed in the test window so we should return false now
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen()); ;

        }

         /// <summary>
         /// Test that if the % of failures is higher than the threshold that the circuit trips.
         /// </summary>
        [TestMethod]
        public void TestTripCircuitOnFailuresAboveThreshold()
        {

            var properties = GetCommandConfig();
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // this should start as allowing requests
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsFalse(cb.IsOpen());

            // success with high latency
            metrics.MarkSuccess();
            metrics.MarkFailure();
            metrics.MarkSuccess();
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();
            Thread.Sleep(properties.MetricsHealthSnapshotIntervalInMilliseconds);

            // this should trip the circuit as the error percentage is above the threshold
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen());

        }

        /// <summary>
        /// Test that if the % of failures is higher than the threshold that the circuit trips.
        /// </summary>
        [TestMethod]
        public void TestCircuitDoesNotTripOnFailuresBelowThreshold()
        {

            var properties = GetCommandConfig();
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // this should start as allowing requests
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsFalse(cb.IsOpen());

            metrics.MarkSuccess();
            metrics.MarkSuccess();
            metrics.MarkSuccess();
            metrics.MarkSuccess();
            metrics.MarkFailure();
            metrics.MarkFailure();

            // this should remain open as the failure threshold is below the percentage limit
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsFalse(cb.IsOpen());

        }


        /// <summary>
        /// Test that if all 'marks' are timeouts that it will trip the circuit.
        /// </summary>
        [TestMethod]
        public void TestTripCircuitOnTimeouts()
        {
            var properties = GetCommandConfig();
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // this should start as allowing requests
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsFalse(cb.IsOpen());

            metrics.MarkTimeout();
            metrics.MarkTimeout();
            metrics.MarkTimeout();
            metrics.MarkTimeout();
            Thread.Sleep(properties.MetricsHealthSnapshotIntervalInMilliseconds);


            // this should remain open as the failure threshold is below the percentage limit
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen());
        }

        /// <summary>
        /// Test that if the % of timeouts is higher than the threshold that the circuit trips.
        /// </summary>
        [TestMethod]
        public void TestTripCircuitOnTimeoutsAboveThreshold()
        {
 
                var properties = GetCommandConfig();
                ICommandMetrics metrics = GetMetrics(properties);
                var cb = new TestCircuitBreaker(properties, metrics);

                // this should start as allowing requests
                Assert.IsTrue(cb.AllowRequest());
                Assert.IsFalse(cb.IsOpen());


                metrics.MarkSuccess();
                metrics.MarkSuccess();
                metrics.MarkSuccess();
                metrics.MarkTimeout();
                metrics.MarkTimeout();
                metrics.MarkTimeout();
                metrics.MarkTimeout();

                Thread.Sleep(properties.MetricsHealthSnapshotIntervalInMilliseconds);
                // this should trip the circuit as the error percentage is above the threshold
                Assert.IsFalse(cb.AllowRequest());
                Assert.IsTrue(cb.IsOpen());
   
        }

        /// <summary>
        /// Test that on an open circuit that a single attempt will be allowed after a window of time to see if issues are resolved.
        /// </summary>
        [TestMethod]
        public virtual void testSingleTestOnOpenCircuitAfterTimeWindow()
        {

            int sleepWindow = 200;
            var properties = GetCommandConfig();
            properties.CircuitBreakerSleepWindowInMilliseconds = sleepWindow;
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // fail
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();

            // everything has failed in the test window so we should return false now
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen());

            // wait for sleepWindow to pass
            Thread.Sleep(sleepWindow + 50);

            // we should now allow 1 request
            Assert.IsTrue(cb.AllowRequest());
            // but the circuit should still be open
            Assert.IsTrue(cb.IsOpen());
            // and further requests are still blocked
            Assert.IsFalse(cb.AllowRequest());

 
        }


        /// <summary>
        /// Test that an open circuit is closed after 1 success.
        /// </summary>
        [TestMethod]
        public void testCircuitClosedAfterSuccess()
        {
            int sleepWindow = 200;
            var properties = GetCommandConfig();
            properties.CircuitBreakerSleepWindowInMilliseconds = sleepWindow;
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // fail
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();

            // everything has failed in the test window so we should return false now
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen());

            // wait for sleepWindow to pass
            Thread.Sleep(sleepWindow + 50);

            // we should now allow 1 request
            Assert.IsTrue(cb.AllowRequest());
            // but the circuit should still be open
            Assert.IsTrue(cb.IsOpen());
            // and further requests are still blocked
            Assert.IsFalse(cb.AllowRequest());

            // the 'singleTest' succeeds so should cause the circuit to be closed
            metrics.MarkSuccess();
            cb.MarkSuccess();

            // all requests should be open again
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsTrue(cb.AllowRequest());
            // and the circuit should be closed again
            Assert.IsFalse(cb.IsOpen());


        }


        /// <summary>
        /// Test that an open circuit is closed after 1 success... when the sleepWindow is smaller than the statisticalWindow and 'failure' stats are still sticking around.
        /// <para>
        /// This means that the statistical window needs to be cleared otherwise it will still calculate the failure percentage below the threshold and immediately open the circuit again.
        /// </para>
        /// </summary>
        [TestMethod]
        public virtual void testCircuitClosedAfterSuccessAndClearsStatisticalWindow()
        {

            int statisticalWindow = 200;
            int sleepWindow = 10; // this is set very low so that returning from a retry still ends up having data in the buckets for the statisticalWindow
            var properties = GetCommandConfig();
            properties.CircuitBreakerSleepWindowInMilliseconds = sleepWindow;
            properties.MetricsRollingStatisticalWindowInMilliseconds = statisticalWindow;
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // fail
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();

            // everything has failed in the test window so we should return false now
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen());

            // wait for sleepWindow to pass
            Thread.Sleep(sleepWindow + 50);

            // we should now allow 1 request
            Assert.IsTrue(cb.AllowRequest());
            // but the circuit should still be open
            Assert.IsTrue(cb.IsOpen());
            // and further requests are still blocked
            Assert.IsFalse(cb.AllowRequest());

            // the 'singleTest' succeeds so should cause the circuit to be closed
            metrics.MarkSuccess();
            cb.MarkSuccess();

            // all requests should be open again
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsTrue(cb.AllowRequest());
            // and the circuit should be closed again
            Assert.IsFalse(cb.IsOpen());

           
        }


        /// <summary>
        /// Over a period of several 'windows' a single attempt will be made and fail and then finally succeed and close the circuit.
        /// <para>
        /// Ensure the circuit is kept open through the entire testing period and that only the single attempt in each window is made.
        /// </para>
        /// </summary>
        [TestMethod]
        public void TestMultipleTimeWindowRetriesBeforeClosingCircuit()
        {

            int sleepWindow = 200;
            var properties = GetCommandConfig();
            properties.CircuitBreakerSleepWindowInMilliseconds = sleepWindow;
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // fail
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();

            // everything has failed in the test window so we should return false now
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsTrue(cb.IsOpen());

            // wait for sleepWindow to pass
            Thread.Sleep(sleepWindow + 50);

            // we should now allow 1 request
            Assert.IsTrue(cb.AllowRequest());
            // but the circuit should still be open
            Assert.IsTrue(cb.IsOpen());
            // and further requests are still blocked
            Assert.IsFalse(cb.AllowRequest());

            // the 'singleTest' fails so it should go back to sleep and not allow any requests again until another 'singleTest' after the sleep
            metrics.MarkFailure();

            Assert.IsFalse(cb.AllowRequest());
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsFalse(cb.AllowRequest());

            // wait for sleepWindow to pass
            Thread.Sleep(sleepWindow + 50);

            // we should now allow 1 request
            Assert.IsTrue(cb.AllowRequest());
            // but the circuit should still be open
            Assert.IsTrue(cb.IsOpen());
            // and further requests are still blocked
            Assert.IsFalse(cb.AllowRequest());

            // the 'singleTest' fails again so it should go back to sleep and not allow any requests again until another 'singleTest' after the sleep
            metrics.MarkFailure();


            Assert.IsFalse(cb.AllowRequest());
            Assert.IsFalse(cb.AllowRequest());
            Assert.IsFalse(cb.AllowRequest());

            // wait for sleepWindow to pass
            Thread.Sleep(sleepWindow + 50);


            // we should now allow 1 request
            Assert.IsTrue(cb.AllowRequest());
            // but the circuit should still be open
            Assert.IsTrue(cb.IsOpen());
            // and further requests are still blocked
            Assert.IsFalse(cb.AllowRequest());

            // now it finally succeeds
            metrics.MarkSuccess();
            cb.MarkSuccess();

            // all requests should be open again
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsTrue(cb.AllowRequest());
            // and the circuit should be closed again
            Assert.IsFalse(cb.IsOpen());

            
        }


        /// <summary>
        /// When volume of reporting during a statistical window is lower than a defined threshold the circuit
        /// will not trip regardless of whatever statistics are calculated.
        /// </summary>
        [TestMethod]
        public void testLowVolumeDoesNotTripCircuit()
        {
            int sleepWindow = 200;
            int lowVolume = 5;

            var properties = GetCommandConfig();
            properties.CircuitBreakerSleepWindowInMilliseconds = sleepWindow;
            properties.CircuitBreakerRequestCountThreshold = lowVolume;
            ICommandMetrics metrics = GetMetrics(properties);
            var cb = new TestCircuitBreaker(properties, metrics);

            // fail
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();
            metrics.MarkFailure();

            // even though it has all failed we won't trip the circuit because the volume is low
            Assert.IsTrue(cb.AllowRequest());
            Assert.IsFalse(cb.IsOpen());

        }

        #endregion


        #region private Method
        private ICommandMetrics GetMetrics(ICommandConfigSet configset)
        {
            return ComponentFactory.CreateCommandMetrics(configset, "BreakerTest", IsolationModeEnum.SemaphoreIsolation);
           
        }

        private CommandConfigSet GetCommandConfig()
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