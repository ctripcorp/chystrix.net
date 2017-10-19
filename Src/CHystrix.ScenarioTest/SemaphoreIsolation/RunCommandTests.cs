using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.SemaphoreIsolation
{
    [TestClass]
    public class RunCommandTests : TestClassBase
    {
        protected override void CustomTestInit()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, config => { });
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
        }

        [TestMethod]
        public void RunWithEmptyKey()
        {
            foreach (string emptyString in EmptyStrings)
            {
                try
                {
                    HystrixCommandBase.RunCommand<string>(emptyString, () => Expected);
                    Assert.Fail("Argument exception should be thrown before.");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [TestMethod]
        public void RunCommandSuccess()
        {
            RunToSuccessNormally();
        }

        [TestMethod]
        public void RunCommandFailed()
        {
            RunToExceptionNormally();
        }

        [TestMethod]
        public void RunCommand_FailFastQuickRecover_CausedByException()
        {
            RunToShortCircuitedCausedByException();

            Recover();

            RunToExceptionNormally();

            RunToTimeoutNormally();
        }

        [TestMethod]
        public void RunCommand_FailFastQuickRecover_CausedByTimeout()
        {
            RunToShortCircuitedCausedByTimeout();

            Recover();

            RunToTimeoutNormally();

            RunToExceptionNormally();
        }

        [TestMethod]
        public void RunCommand_FailFast_ContinueFail_CausedByException()
        {
            RunToShortCircuitedCausedByException();

            WaitSleepWindowBeforeAllowSingleRequest();

            RunToExceptionNormally();

            RunToExceptionShortCircuited();
            RunToTimeoutShortCircuited();
        }

        [TestMethod]
        public void RunCommand_FailFast_ContinueFail_CausedByTimeout()
        {
            RunToShortCircuitedCausedByTimeout();

            WaitSleepWindowBeforeAllowSingleRequest();

            RunToTimeoutNormally();

            RunToTimeoutShortCircuited();
            RunToExceptionShortCircuited();
        }

        [TestMethod]
        public void RunCommand_FailFastQuickRecover_CausedByRandomReason()
        {
            RunToShortCircuitedCausedByRandomReason();

            Recover();

            RunToFailNormallyCausedByRandomReason();
            RunToSuccessNormally();
        }

        [TestMethod]
        public void RunCommand_FailFast_ContinueFail_CausedByRandomReason()
        {
            RunToShortCircuitedCausedByRandomReason();

            WaitSleepWindowBeforeAllowSingleRequest();

            RunToFailNormallyCausedByRandomReason();

            RunToFailShortCircuitedCausedByRandomReason();
            RunToFailShortCircuitedCausedByRandomReason();
        }

        [TestMethod]
        public void RunCommand_NotCareBadRequestException()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold * 2; i++)
            {
                RunToBadRequestExceptionNormally();
            }
        }

        [TestMethod]
        public void RunCommand_NotCareBadRequestException2()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold * 2; i++)
            {
                RunToCompleteCausedByRandomReason(withFail: false);
            }
        }

        [TestMethod]
        public void SimulateRealScenario()
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
                RunToCompleteCausedByRandomReason(withFail: false);
            }

            for (int i = 0; i < failRequestCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToExceptionNormally();
            }

            for (int i = 0; i < badRequestCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToBadRequestExceptionNormally();
            }

            for (int i = 0; i < successCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToSuccessNormally();
            }

            for (int i = 0; i < timeoutRequestCount; i++)
            {
                Thread.Sleep(requestInterval);
                RunToTimeoutNormally();
            }

            bool shortCircuited = false;
            for (int i = 0; i < moreRequestCount; i++)
            {
                try
                {
                    RunToFailNormallyCausedByRandomReason();
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
                RunToShortCircuitedCausedByRandomReasonAfterCircuitBreakerOpen();
            }

            Thread.Sleep(2000);
            RunToSuccessNormally();

            RunToCompleteCausedByRandomReason();
            RunToCompleteCausedByRandomReason();
        }

        #region Privates

        private void WaitSleepWindowBeforeAllowSingleRequest()
        {
            Thread.Sleep(CommandComponents.ConfigSet.CircuitBreakerSleepWindowInMilliseconds + 100);
        }

        private string ExecuteTimeout()
        {
            Thread.Sleep(TimeoutInMilliseconds + 2);
            return Expected;
        }

        private void RunToShortCircuitedCausedByException()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToExceptionNormally();
            }

            RunToExceptionShortCircuited();
        }

        private void RunToShortCircuitedCausedByTimeout()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToTimeoutNormally();
            }

            RunToTimeoutShortCircuited();
        }

        private void RunToShortCircuitedCausedByRandomReason()
        {
            for (int i = 0; i < CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                RunToFailNormallyCausedByRandomReason();
            }

            RunToFailShortCircuitedCausedByRandomReason();
        }

        private void Recover()
        {
            WaitSleepWindowBeforeAllowSingleRequest();
            RunToSuccessNormally();
        }

        private void RunToBadRequestExceptionNormally()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new BadRequestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (BadRequestException)
            {
            }
        }

        private void RunToExceptionNormally()
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

        private void RunToTimeoutNormally()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            string actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, ExecuteTimeout);
            Assert.AreEqual(Expected, actual);
        }

        private void RunToSuccessNormally()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            string actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => Expected);
            Assert.AreEqual(Expected, actual);
        }

        private void RunToFailNormallyCausedByRandomReason()
        {
            int random = ScenarioTestHelper.Random.Next();
            if (random % 2 == 0)
                RunToExceptionNormally();
            else
                RunToTimeoutNormally();
        }

        private void RunToSuccessShortCircuited()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => Expected);
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToBadRequestExceptionShortCircuited()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new BadRequestException(); });
                Assert.Fail("Execution should throw exception.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToExceptionShortCircuited()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
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

        private void RunToTimeoutShortCircuited()
        {
            ScenarioTestHelper.SleepHealthSnapshotInverval();
            try
            {
                HystrixCommandBase.RunCommand<string>(TestCommandKey, ExecuteTimeout);
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        private void RunToFailShortCircuitedCausedByRandomReason()
        {
            int random = ScenarioTestHelper.Random.Next();
            if (random % 2 == 0)
                RunToExceptionShortCircuited();
            else
                RunToTimeoutShortCircuited();
        }

        private void RunToShortCircuitedCausedByRandomReasonAfterCircuitBreakerOpen()
        {
            int random = ScenarioTestHelper.Random.Next() % 4;
            switch (random)
            {
                case 0:
                    RunToSuccessShortCircuited();
                    break;
                case 1:
                    RunToBadRequestExceptionShortCircuited();
                    break;
                case 2:
                    RunToExceptionShortCircuited();
                    break;
                case 3:
                    RunToTimeoutShortCircuited();
                    break;
            }
        }

        private bool RunToCompleteCausedByRandomReason(bool withFail = false)
        {
            bool fail = false;

            int random = ScenarioTestHelper.Random.Next() % (withFail ? 4 : 2);
            switch(random)
            {
                case 0:
                    RunToSuccessNormally();
                    break;
                case 1:
                    RunToBadRequestExceptionNormally();
                    break;
                case 2:
                    RunToExceptionNormally();
                    fail = true;
                    break;
                case 3:
                    RunToTimeoutNormally();
                    fail = true;
                    break;
            }

            return fail;
        }

        #endregion
    }
}
