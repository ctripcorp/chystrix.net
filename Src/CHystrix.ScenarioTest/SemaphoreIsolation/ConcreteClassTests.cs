using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.SemaphoreIsolation
{
    [TestClass]
    public class ConcreteClassTests : TestClassBase
    {
        protected override void CustomTestInit()
        {
            new SampleIsolationCommand(TestCommandKey);
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
        }

        [TestMethod]
        public void RunToShortCircuited_FromConcreteClass()
        {
            int i = 0;
            for (i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToTimeoutNormallyFromConcreteClass();
            }

            RunToTimeoutShortCircuitedFromConcreteClass();
        }

        [TestMethod]
        public void RunCommandSuccess_FromConcreteClass()
        {
            RunToSuccessNormallyFromConcreteClass();
        }

        [TestMethod]
        public void RunCommandFailed_FromConcreteClass()
        {
            RunToExceptionNormallyFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_FailFastQuickRecover_CausedByException_FromConcreteClass()
        {
            RunToShortCircuitedCausedByExceptionFromConcreteClass();

            RecoverFromConcreteClass();

            RunToExceptionNormallyFromConcreteClass();

            RunToTimeoutNormallyFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_FailFastQuickRecover_CausedByTimeout_FromConcreteClass()
        {
            RunToShortCircuitedCausedByTimeoutFromConcreteClass();

            RecoverFromConcreteClass();

            RunToTimeoutNormallyFromConcreteClass();

            RunToExceptionNormallyFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_FailFast_ContinueFail_CausedByException_FromConcreteClass()
        {
            RunToShortCircuitedCausedByExceptionFromConcreteClass();

            WaitSleepWindowBeforeAllowSingleRequestFromConcreteClass();

            RunToExceptionNormallyFromConcreteClass();

            RunToExceptionShortCircuitedFromConcreteClass();
            RunToTimeoutShortCircuitedFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_FailFast_ContinueFail_CausedByTimeout_FromConcreteClass()
        {
            RunToShortCircuitedCausedByTimeoutFromConcreteClass();

            WaitSleepWindowBeforeAllowSingleRequestFromConcreteClass();

            RunToTimeoutNormallyFromConcreteClass();

            RunToTimeoutShortCircuitedFromConcreteClass();
            RunToExceptionShortCircuitedFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_FailFastQuickRecover_CausedByRandomReason_FromConcreteClass()
        {
            RunToShortCircuitedCausedByRandomReasonFromConcreteClass();

            RecoverFromConcreteClass();

            RunToFailNormallyCausedByRandomReasonFromConcreteClass();
            RunToSuccessNormallyFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_FailFast_ContinueFail_CausedByRandomReason_FromConcreteClass()
        {
            RunToShortCircuitedCausedByRandomReasonFromConcreteClass();

            WaitSleepWindowBeforeAllowSingleRequestFromConcreteClass();

            RunToFailNormallyCausedByRandomReasonFromConcreteClass();

            RunToFailShortCircuitedCausedByRandomReasonFromConcreteClass();
            RunToFailShortCircuitedCausedByRandomReasonFromConcreteClass();
        }

        [TestMethod]
        public void RunCommand_NotCareBadRequestException_FromConcreteClass()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold * 2; i++)
            {
                RunToBadRequestExceptionNormallyFromConcreteClass();
            }
        }

        [TestMethod]
        public void RunCommand_NotCareBadRequestException2_FromConcreteClass()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold * 2; i++)
            {
                RunToCompleteCausedByRandomReasonFromConcreteClass(withFail: false);
            }
        }

        [TestMethod]
        public void SimulateRealScenario_FromConcreteClass()
        {
            int totalCount = CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold;
            int randomNonFailCount = (int)(totalCount * 0.2);
            int successCount = (int)(totalCount * 0.2);
            int badRequestCount = (int)(totalCount * 0.1);
            int timeoutRequestCount = (int)(totalCount * 0.4);
            int failRequestCount = (int)(totalCount * 0.1);

            int moreRequestCount = (int)(totalCount * 0.4);
            int requestInterval = 3;

            for (int i = 0; i < randomNonFailCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToCompleteCausedByRandomReasonFromConcreteClass(withFail: false);
            }

            for (int i = 0; i < failRequestCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToExceptionNormallyFromConcreteClass();
            }

            for (int i = 0; i < badRequestCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToBadRequestExceptionNormallyFromConcreteClass();
            }

            for (int i = 0; i < successCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToSuccessNormallyFromConcreteClass();
            }

            for (int i = 0; i < timeoutRequestCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToTimeoutNormallyFromConcreteClass();
            }

            bool shortCircuited = false;
            for (int i = 0; i < moreRequestCount; i++)
            {
                try
                {
                    RunToFailNormallyCausedByRandomReasonFromConcreteClass();
                }
                catch (HystrixException ex)
                {
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                    shortCircuited = true;
                }
            }

            Assert.IsTrue(shortCircuited);

            for (int i = 0; i < 4; i++)
            {
                Thread.Sleep(1000);
                RunToShortCircuitedCausedByRandomReasonAfterCircuitBreakerOpenFromConcreteClass();
            }

            Thread.Sleep(2000);
            RunToSuccessNormallyFromConcreteClass();

            RunToCompleteCausedByRandomReasonFromConcreteClass();
            RunToCompleteCausedByRandomReasonFromConcreteClass();
        }

        #region Privates

        private void WaitSleepWindowBeforeAllowSingleRequestFromConcreteClass()
        {
            Thread.Sleep(CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds + 100);
        }

        private void RunToShortCircuitedCausedByExceptionFromConcreteClass()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToExceptionNormallyFromConcreteClass();
            }

            RunToExceptionShortCircuitedFromConcreteClass();
        }

        private void RunToShortCircuitedCausedByTimeoutFromConcreteClass()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToTimeoutNormallyFromConcreteClass();
            }

            RunToTimeoutShortCircuitedFromConcreteClass();
        }

        private void RunToShortCircuitedCausedByRandomReasonFromConcreteClass()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToFailNormallyCausedByRandomReasonFromConcreteClass();
            }

            RunToFailShortCircuitedCausedByRandomReasonFromConcreteClass();
        }

        private void RecoverFromConcreteClass()
        {
            WaitSleepWindowBeforeAllowSingleRequestFromConcreteClass();
            RunToSuccessNormallyFromConcreteClass();
        }

        private void RunToBadRequestExceptionNormallyFromConcreteClass()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new BadRequestException(); });
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                command.Run();
                Assert.Fail("Execution should throw exception.");
            }
            catch (BadRequestException)
            {
            }
        }

        private void RunToExceptionNormallyFromConcreteClass()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new ScenarioTestException(); });
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                command.Run();
                Assert.Fail("Execution should throw exception.");
            }
            catch (ScenarioTestException)
            {
            }
        }

        private void RunToTimeoutNormallyFromConcreteClass()
        {
            SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, expectedResult: Expected,
                sleepTimeInMilliseconds: TimeoutInMilliseconds + 2);
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            string actual = command.Run();
            Assert.AreEqual(Expected, actual);
        }

        private void RunToSuccessNormallyFromConcreteClass()
        {
            SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => Expected);
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            string actual = command.Run();
            Assert.AreEqual(Expected, actual);
        }

        private void RunToFailNormallyCausedByRandomReasonFromConcreteClass()
        {
            int random = ScenarioTestHelper.Random.Next();
            if (random % 2 == 0)
                RunToExceptionNormallyFromConcreteClass();
            else
                RunToTimeoutNormallyFromConcreteClass();
        }

        private void RunToSuccessShortCircuitedFromConcreteClass()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => Expected);
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                command.Run();
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToBadRequestExceptionShortCircuitedFromConcreteClass()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new BadRequestException(); });
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                command.Run();
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToExceptionShortCircuitedFromConcreteClass()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, execute: () => { throw new ScenarioTestException(); });
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                command.Run();
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToTimeoutShortCircuitedFromConcreteClass()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                SampleIsolationCommand command = new SampleIsolationCommand(TestCommandKey, sleepTimeInMilliseconds: TimeoutInMilliseconds + 2);
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                command.Run();
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToFailShortCircuitedCausedByRandomReasonFromConcreteClass()
        {
            int random = ScenarioTestHelper.Random.Next();
            if (random % 2 == 0)
                RunToExceptionShortCircuitedFromConcreteClass();
            else
                RunToTimeoutShortCircuitedFromConcreteClass();
        }

        private void RunToShortCircuitedCausedByRandomReasonAfterCircuitBreakerOpenFromConcreteClass()
        {
            int random = ScenarioTestHelper.Random.Next() % 4;
            switch (random)
            {
                case 0:
                    RunToSuccessShortCircuitedFromConcreteClass();
                    break;
                case 1:
                    RunToBadRequestExceptionShortCircuitedFromConcreteClass();
                    break;
                case 2:
                    RunToExceptionShortCircuitedFromConcreteClass();
                    break;
                case 3:
                    RunToTimeoutShortCircuitedFromConcreteClass();
                    break;
            }
        }

        private bool RunToCompleteCausedByRandomReasonFromConcreteClass(bool withFail = false)
        {
            bool fail = false;

            int random = ScenarioTestHelper.Random.Next() % (withFail ? 4 : 2);
            switch(random)
            {
                case 0:
                    RunToSuccessNormallyFromConcreteClass();
                    break;
                case 1:
                    RunToBadRequestExceptionNormallyFromConcreteClass();
                    break;
                case 2:
                    RunToExceptionNormallyFromConcreteClass();
                    fail = true;
                    break;
                case 3:
                    RunToTimeoutNormallyFromConcreteClass();
                    fail = true;
                    break;
            }

            return fail;
        }

        #endregion
    }
}
