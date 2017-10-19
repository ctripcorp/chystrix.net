using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CHystrix.Utils;
using CHystrix.Utils.Atomic;

namespace CHystrix.CircuitBreaker
{
    internal class CircuitBreaker : ICircuitBreaker
    {
        protected readonly ICommandConfigSet ConfigSet;
        protected readonly ICommandMetrics Metrics;

        protected readonly AtomicBoolean OpenFlag;
        protected readonly AtomicLong CircuitOpenedOrLastTestedTime;

        public CircuitBreaker(ICommandConfigSet configSet, ICommandMetrics metrics)
        {
            ConfigSet = configSet;
            Metrics = metrics;

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

            if (OpenFlag)
                return true;

            CommandExecutionHealthSnapshot healthSnapshot = Metrics.GetExecutionHealthSnapshot();
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
                    new Dictionary<string, string>() { { "CircuitBreaker", "Open" } }.AddLogTagData("FXD303010"));
            }

            return true;
        }

        public void MarkSuccess()
        {
            if (!ConfigSet.CircuitBreakerEnabled)
                return;

            if (OpenFlag)
            {
                OpenFlag.Value = false;
                Metrics.Reset();
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
                    CommonUtils.Log.Log(
                        LogLevelEnum.Info,
                        ConfigSet.CircuitBreakerSleepWindowInMilliseconds + " milliseconds passed. Allow 1 request to run to see whether the access point has recovered.",
                        new Dictionary<string, string>()
                        {
                            { "CircuitBreaker", "AllowSingleRequest" }
                        });

                    return true;
                }
            }

            return false;
        }
    }
}
