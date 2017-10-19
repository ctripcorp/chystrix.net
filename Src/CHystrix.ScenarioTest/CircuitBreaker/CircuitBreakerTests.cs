using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CHystrix;
using CHystrix.Config;
using CircuitBreakerImpl = CHystrix.CircuitBreaker.CircuitBreaker;
using CHystrix.Metrics;

namespace CHystrix.ScenarioTest.CircuitBreaker
{
    [TestClass]
    public class CircuitBreakerTests : TestClassBase
    {
        protected override void CustomTestInit()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, config => { });
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
        }

        [TestMethod]
        public void CircuitBreakerEnabledSettingIsFalse()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerEnabled = false;

            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold * 3; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Assert.AreEqual(false, CommandComponents.CircuitBreaker.IsOpen());
                Assert.AreEqual(true, CommandComponents.CircuitBreaker.AllowRequest());

                try
                {
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                }
            }
        }

        [TestMethod]
        public void CircuitBreakerForceOpenIsTrueWithoutFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceOpen = true;
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => string.Empty);
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void CircuitBreakerForceOpenIsTrueWithFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceOpen = true;
            string result = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => string.Empty, () => null);
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void CircuitBreakerForceOpenIsFalseWithoutFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceOpen = false;
            string result = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void CircuitBreakerForceOpenIsFalseWithFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceOpen = false;
            string result = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => string.Empty, () => null);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void CircuitBreakerForceClosedIsTrueWithoutFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceClosed = true;

            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                try
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                }
            }

            try
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (ScenarioTestException)
            {
            }
        }

        [TestMethod]
        public void CircuitBreakerForceClosedIsTrueWithFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceClosed = true;

            string fallback = null;
            string actual;
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); }, () => fallback);
                Assert.AreEqual(fallback, actual);
            }

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); }, () => fallback);
            Assert.AreEqual(fallback, actual);
        }

        [TestMethod]
        public void CircuitBreakerForceClosedIsFalseWithoutFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceClosed = false;

            string expected = string.Empty;
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                try
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                }
            }

            try
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void CircuitBreakerForceClosedIsFalseWithFallback()
        {
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerForceClosed = false;

            string fallback = null;
            string actual;
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); }, () => fallback);
                Assert.AreEqual(fallback, actual);
            }

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); }, () => fallback);
            Assert.AreEqual(fallback, actual);
        }

        [TestMethod]
        public void CircuitBreakerHasDefaultRequestCountThresholdSetting()
        {
            TestRequestCountThresholdSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasSmallRequestCountThresholdSetting()
        {
            TestRequestCountThresholdSetting(1);
        }

        [TestMethod]
        public void CircuitBreakerHasLargeRequestCountThresholdSetting()
        {
            TestRequestCountThresholdSetting(100);
        }

        [TestMethod]
        public void CircuitBreakerHasInvalidRequestCountThresholdSetting()
        {
            int defaultThreshold = CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold;
            int invalid = ScenarioTestHelper.Random.Next(int.MinValue, 1);
            CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold = invalid;
            Assert.AreEqual(defaultThreshold, CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold);
            Assert.AreNotEqual(defaultThreshold, invalid);
            TestRequestCountThresholdSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasDefaultErrorPercentageThresholdSetting()
        {
            TestErrorPercentageThresholdSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasSmallErrorPercentageThresholdSetting()
        {
            TestErrorPercentageThresholdSetting(1);
        }

        [TestMethod]
        public void CircuitBreakerHasLargeErrorPercentageThresholdSetting()
        {
            TestErrorPercentageThresholdSetting(99);
        }

        [TestMethod]
        public void CircuitBreakerHasLargeErrorPercentageThresholdSetting100()
        {
            TestErrorPercentageThresholdSetting(100);
        }

        [TestMethod]
        public void CircuitBreakerHasInvalidErrorPercentageThresholdSetting()
        {
            int defaultThreshold = CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage;
            int invalid = ScenarioTestHelper.Random.Next(int.MinValue, 1);
            CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage = invalid;
            Assert.AreEqual(defaultThreshold, CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage);
            Assert.AreNotEqual(defaultThreshold, invalid);
            TestErrorPercentageThresholdSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasInvalidErrorPercentageThresholdSettingLargeThan100()
        {
            int defaultThreshold = CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage;
            int invalid = ScenarioTestHelper.Random.Next(101, int.MaxValue);
            CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage = invalid;
            Assert.AreEqual(defaultThreshold, CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage);
            Assert.AreNotEqual(defaultThreshold, invalid);
            TestErrorPercentageThresholdSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasDefaultSleepWindowSettingAndTestSuccess()
        {
            TestSleepWindowSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasDefaultSleepWindowSettingAndTestFail()
        {
            TestSleepWindowSetting(autoTestSuccess: false);
        }

        [TestMethod]
        public void CircuitBreakerHasSmallSleepWindowSettingAndTestSuccess()
        {
            TestSleepWindowSetting(1);
        }

        [TestMethod]
        public void CircuitBreakerHasSmallSleepWindowSettingAndTestFail()
        {
            TestSleepWindowSetting(1, false);
        }

        [TestMethod]
        public void CircuitBreakerHasLargeSleepWindowSettingAndTestSuccess()
        {
            TestSleepWindowSetting(100);
        }

        [TestMethod]
        public void CircuitBreakerHasLargeSleepWindowSettingAndTestFail()
        {
            TestSleepWindowSetting(100, false);
        }

        [TestMethod]
        public void CircuitBreakerHasInvalidSleepWindowSettingAndTestSuccess()
        {
            int defaultSleepWindow = CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds;
            int invalid = ScenarioTestHelper.Random.Next(int.MinValue, 1);
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerSleepWindowInMilliseconds = invalid;
            Assert.AreEqual(defaultSleepWindow, CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds);
            Assert.AreNotEqual(defaultSleepWindow, invalid);
            TestSleepWindowSetting();
        }

        [TestMethod]
        public void CircuitBreakerHasInvalidSleepWindowSettingAndTestFail()
        {
            int defaultSleepWindow = CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds;
            int invalid = ScenarioTestHelper.Random.Next(int.MinValue, 1);
            CommandComponents.ConfigSet.ToConcrete().CircuitBreakerSleepWindowInMilliseconds = invalid;
            Assert.AreEqual(defaultSleepWindow, CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds);
            Assert.AreNotEqual(defaultSleepWindow, invalid);
            TestSleepWindowSetting(autoTestSuccess: false);
        }

        [TestMethod]
        public void CircuitBreakerOpenCausedByTimeout()
        {
            int timeoutInMilliseconds = 10;
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = timeoutInMilliseconds;

            string expected = string.Empty;
            string actual;
            Func<string> execute = () =>
                {
                    Thread.Sleep(timeoutInMilliseconds + 2);
                    return expected;
                };

            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, execute);
                Assert.AreEqual(expected, actual);
            }

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, execute);
                    var snapshot = CommandComponents.Metrics.GetExecutionEventDistribution();
                    Assert.Fail("Execution should throw exception.");
                }
                catch (HystrixException ex)
                {
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                }
            }

            Thread.Sleep(CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds + 1);
            actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, execute);
            Assert.AreEqual(expected, actual);
            try
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                HystrixCommandBase.RunCommand<string>(TestCommandKey, execute);
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }

            Thread.Sleep(CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds + 1);
            actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => expected);
            Assert.AreEqual(expected, actual);
        }

        #region privates

        private void TestRequestCountThresholdSetting(int? setting = null)
        {
            if (setting.HasValue)
                CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold = setting.Value;
            else
                setting = CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold;

            for (int i = 0; i < setting.Value; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                try
                {
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                }
            }

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException)
            {
            }
        }

        private void TestErrorPercentageThresholdSetting(int? setting = null)
        {
            if (setting.HasValue)
                CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage = setting.Value;
            else
                setting = CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage;

            if (setting.Value == 100)
            {
                TestErrorPercentageThresholdSetting100();
                return;
            }

            int errorCount = setting.Value;
            int successCount = 100 - errorCount;
            for (int i = 0; i < successCount; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => null);
            }

            for (int i = 0; i < errorCount; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                try
                {
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                }
            }

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => null);
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void TestErrorPercentageThresholdSetting100()
        {
            CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage = 100;

            for (int i = 0; i < 100; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                try
                {
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                    if (i >= CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold)
                        Assert.Fail("Execution should throw HystrixException.");
                }
                catch (HystrixException ex)
                {
                    if (i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold)
                        Assert.Fail("Execution should throw ScenarioTestException.");
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                }
            }
        }

        private void TestSleepWindowSetting(int? setting = null, bool autoTestSuccess = true)
        {
            if (setting.HasValue)
                CommandComponents.ConfigSet.ToConcrete().CircuitBreakerSleepWindowInMilliseconds = setting.Value;
            else
                setting = CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds;

            int errorCount = CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage;
            int successCount = 100 - errorCount;
            for (int i = 0; i < successCount; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => null);
            }

            for (int i = 0; i < errorCount; i++)
            {
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                try
                {
                    HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                    Assert.Fail("Execution should throw exception.");
                }
                catch (ScenarioTestException)
                {
                }
            }

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => null);
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
 
            Thread.Sleep(CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds + 1);

            if (autoTestSuccess)
            {
                string result = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => null);
                Assert.AreEqual(null, result);
                return;
            }

            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (ScenarioTestException)
            {
            }

            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new ScenarioTestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        #endregion
    }
}
