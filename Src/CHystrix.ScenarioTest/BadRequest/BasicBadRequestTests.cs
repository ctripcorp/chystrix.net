using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CHystrix.Utils;

namespace CHystrix.ScenarioTest.BadRequest
{
    [TestClass]
    public class BasicBadRequestTests : TestClassBase
    {
        protected override void CustomTestInit()
        {
            new SampleIsolationCommand(TestCommandKey);
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
        }

        [TestMethod]
        public void BadRequestRecordBadRequestMetrics()
        {
            for (int i = 1; i <= 3; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new BadRequestException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(BadRequestException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);
            }
        }

        [TestMethod]
        public void BadRequestWillExecuteFallback()
        {
            SampleHasFallbackIsolationCommand command = new SampleHasFallbackIsolationCommand(
                TestCommandKey,
                execute: () => { throw new BadRequestException(); },
                fallback: () => string.Empty);
            command.Run();
        }
 
        [TestMethod]
        public void BadRequestNotCauseCircuitBreakerOpen()
        {
            CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold = 2;
            for (int i = 0; i < 100; i++)
            {
                SampleIsolationCommand command = new SampleIsolationCommand(
                    TestCommandKey,
                    execute: () => { throw new BadRequestException(); });
                try
                {
                    command.Run();
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(ex, typeof(BadRequestException));
                }

                ScenarioTestHelper.SleepHealthSnapshotInverval();
                Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                Assert.AreEqual(0, counts[CommandExecutionEventEnum.ShortCircuited]);
            }
        }

        [TestMethod]
        public void IsBadRequestException_NullException()
        {
            Exception ex = null;
            Assert.IsFalse(ex.IsBadRequestException());
        }

        [TestMethod]
        public void IsBadRequestException_BadRequestException()
        {
            Exception ex = new BadRequestException();
            Assert.IsTrue(ex.IsBadRequestException());
        }
       
        [TestMethod]
        public void IsBadRequestException_NonBadRequestException()
        {
            Exception ex = new Exception();
            Assert.IsFalse(ex.IsBadRequestException());
        }

        [TestMethod]
        public void IsBadRequestException_CustomBadRequestException()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker("custom", ex => ex is IndexOutOfRangeException);
            Exception ex2 = new IndexOutOfRangeException();
            Assert.IsTrue(ex2.IsBadRequestException());
        }
       
        [TestMethod]
        public void IsBadRequestException_NonCustomBadRequestException()
        {
            HystrixCommandBase.RegisterCustomBadRequestExceptionChecker("custom", ex => ex is IndexOutOfRangeException);
            Exception ex2 = new Exception();
            Assert.IsFalse(ex2.IsBadRequestException());
        }
    }
}
