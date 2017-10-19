using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.BadRequest
{
    [TestClass]
    public class CustomBadRequestTests : TestClassBase
    {
        private const string NormalCheckerName = "NormalChecker";
        private const string WithExceptionCheckerName = "WithExceptionChecker";

        private const string NormalCheckerName2 = "NormalChecker2";
        private const string WithExceptionCheckerName2 = "WithExceptionChecker2";

        private static Func<Exception, bool> IsBadRequestException = ex =>
        {
            return ex is ArgumentOutOfRangeException;
        };

        private static Func<Exception, bool> IsBadRequestExceptionWithException = ex =>
        {
            throw new Exception();
        };

        private static Func<Exception, bool> IsBadRequestException2 = ex =>
        {
            return ex is IndexOutOfRangeException;
        };

        private static Func<Exception, bool> IsBadRequestExceptionWithException2 = ex =>
        {
            throw new InsufficientMemoryException();
        };

        protected override void CustomTestInit()
        {
            new SampleIsolationCommand(TestCommandKey);
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
        }

        [TestMethod]
        public void RegisterCustomBadRequestExceptionChecker_AddCustomCheckerSuccess()
        {
            Assert.AreEqual(0, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers.Count);
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);
            Assert.AreEqual(1, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers.Count);
            Assert.AreEqual(IsBadRequestException, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName]);

            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName, IsBadRequestExceptionWithException);
            Assert.AreEqual(2, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers.Count);
            Assert.AreEqual(IsBadRequestException, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName]);
            Assert.AreEqual(IsBadRequestExceptionWithException, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[WithExceptionCheckerName]);
        }

        [TestMethod]
        public void RegisterCustomBadRequestExceptionChecker_NullNameParameterCausesArgumentNullException()
        {
            List<string> nullNames = new List<string>()
            {
                null, string.Empty, " ", "\t", "\n"
            };

            foreach (string name in nullNames)
            {
                try
                {
                    HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(name, IsBadRequestException);
                    Assert.Fail("ArgumentNullException should be thrown before this.");
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentNullException), "Exception should be ArgumentNullException");
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RegisterCustomBadRequestExceptionChecker_NullCheckerParameterCausesArgumentNullException()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, null);
        }

        [TestMethod]
        public void RegisterCustomBadRequestExceptionChecker_IgnoreNameCase()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);
            Assert.AreEqual(1, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers.Count);
            Assert.AreEqual(IsBadRequestException, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName]);
            Assert.AreEqual(CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName],
                CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName.ToUpper()]);
            Assert.AreEqual(CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName],
                CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName.ToLower()]);
        }

        [TestMethod]
        public void RegisterCustomBadRequestExceptionChecker_MultiAddWithSameNameOnlyUseTheFirstAddAndIngoreTheLatters()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestExceptionWithException);
            Assert.AreNotEqual(IsBadRequestException, IsBadRequestExceptionWithException);
            Assert.AreEqual(IsBadRequestException, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName]);
            Assert.AreNotEqual(IsBadRequestExceptionWithException, CustomBadRequestExceptionChecker.BadRequestExceptionCheckers[NormalCheckerName]);
        }

        [TestMethod]
        public void CustomBadRequestRecordBadRequestMetrics()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);

            for (int i = 1; i <= 3; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new ArgumentOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);
            }
        }

        [TestMethod]
        public void CustomBadRequestExecuteFallback()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);

            SampleHasFallbackIsolationCommand command = new SampleHasFallbackIsolationCommand(
                TestCommandKey,
                execute: () => { throw new ArgumentOutOfRangeException(); },
                fallback: () => string.Empty);
            command.Run();
        }
 
        [TestMethod]
        public void CustomBadRequestNotCauseCircuitBreakerOpen()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);

            CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold = 2;
            for (int i = 0; i < 100; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(
                    TestCommandKey,
                    execute: () => { throw new ArgumentOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(0, counts[CommandExecutionEventEnum.ShortCircuited]);
            }
        }

        [TestMethod]
        public void CustomBadRequestRecordBadRequestMetrics_CheckerThrowException()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName, IsBadRequestExceptionWithException);

            for (int i = 1; i <= 3; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new ArgumentOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(0, counts[CommandExecutionEventEnum.BadRequest]);
                Assert.AreEqual(i, counts[CommandExecutionEventEnum.Failed]);
            }
        }

        [TestMethod]
        public void CustomBadRequestNotExecuteFallback_CheckerThrowException()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName, IsBadRequestExceptionWithException);

            SampleHasFallbackIsolationCommand command = new SampleHasFallbackIsolationCommand(
                TestCommandKey,
                execute: () => { throw new ArgumentOutOfRangeException(); },
                fallback: () => string.Empty);
            string result = command.Run();
            Assert.AreEqual(string.Empty, result);
        }
 
        [TestMethod]
        public void CustomBadRequestNotCauseCircuitBreakerOpen_CheckerThrowException()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName, IsBadRequestExceptionWithException);

            CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold = 2;
            int circuitBreakerCount = 0;

            for (int i = 0; i < 100; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(
                    TestCommandKey,
                    execute: () => { throw new ArgumentOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                catch (HystrixException)
                {
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                circuitBreakerCount += counts[CommandExecutionEventEnum.ShortCircuited];
            }

            Assert.AreNotEqual(0, circuitBreakerCount);
        }

        [TestMethod]
        public void CustomBadRequestRecordBadRequestMetrics_2GoodCheckers()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName2, IsBadRequestException2);

            for (int i = 1; i <= 3; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new IndexOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(IndexOutOfRangeException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);
            }
        }

        [TestMethod]
        public void CustomBadRequestRecordBadRequestMetrics_1BadChecker_1GoodChecker()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName, IsBadRequestExceptionWithException);
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(NormalCheckerName, IsBadRequestException);

            for (int i = 1; i <= 3; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new ArgumentOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);
            }
        }

        [TestMethod]
        public void CustomBadRequestRecordBadRequestMetrics_2BadCheckers()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName, IsBadRequestExceptionWithException);
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker(WithExceptionCheckerName2, IsBadRequestExceptionWithException2);

            for (int i = 1; i <= 3; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new ArgumentOutOfRangeException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(0, counts[CommandExecutionEventEnum.BadRequest]);
                Assert.AreEqual(i, counts[CommandExecutionEventEnum.Failed]);
            }
        }
    }
}
