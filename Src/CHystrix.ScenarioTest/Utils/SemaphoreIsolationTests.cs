using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CHystrix.Utils;
using SL = CHystrix.Utils.SemaphoreIsolation;

namespace CHystrix.ScenarioTest.Utils
{
    [TestClass]
    public class SemaphoreIsolationTests : TestClassBase
    {
        [TestMethod]
        public void Config_EmptyKey()
        {
            foreach (string key in EmptyStrings)
            {
                try
                {
                    SL.Config(key, null, null, config => { });
                    Assert.Fail("Empty key should cause ArgumentNullException");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [TestMethod]
        public void Config_Empty_Group_Domain_Config()
        {
            SL.Config(TestCommandKey, null, null, default(Action<ICommandConfigSet>));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Group_Domain_Config_Default()
        {
            SL.Config(TestCommandKey, null, null, config => {});
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Group_Domain_Config_Custom()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain, configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Config()
        {
            SL.Config(TestCommandKey, configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Group_Config()
        {
            SL.Config(TestCommandKey, TestGroupKey, configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Group()
        {
            SL.Config(TestCommandKey, TestGroupKey);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Group_Domain()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void Config_Key_Group_Domain_MaxConcurrentCount()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain, CustomConfigSet.CommandMaxConcurrentCount);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.AreEqual(CustomConfigSet.CommandMaxConcurrentCount, CommandComponents.ConfigSet.CommandMaxConcurrentCount);
        }

        [TestMethod]
        public void Config_Key_Group_Domain_MaxConcurrentCount_Timeout()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain, CustomConfigSet.CommandMaxConcurrentCount, CustomConfigSet.CommandTimeoutInMilliseconds);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.AreEqual(CustomConfigSet.CommandMaxConcurrentCount, CommandComponents.ConfigSet.CommandMaxConcurrentCount);
            Assert.AreEqual(CustomConfigSet.CommandTimeoutInMilliseconds, CommandComponents.ConfigSet.CommandTimeoutInMilliseconds);
        }

        [TestMethod]
        public void Config_Key_Group_Domain_MaxConcurrentCount_Timeout_RequestCountThreshold_ErrorThresholdPercentage_FallbackMaxConcurrentCount()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain,
                CustomConfigSet.CommandMaxConcurrentCount, CustomConfigSet.CommandTimeoutInMilliseconds,
                CustomConfigSet.CircuitBreakerRequestCountThreshold, CustomConfigSet.CircuitBreakerErrorThresholdPercentage,
                CustomConfigSet.FallbackMaxConcurrentCount);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsFalse(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
            Assert.AreEqual(CustomConfigSet.CommandMaxConcurrentCount, CommandComponents.ConfigSet.CommandMaxConcurrentCount);
            Assert.AreEqual(CustomConfigSet.CommandTimeoutInMilliseconds, CommandComponents.ConfigSet.CommandTimeoutInMilliseconds);
            Assert.AreEqual(CustomConfigSet.CircuitBreakerRequestCountThreshold, CommandComponents.ConfigSet.CircuitBreakerRequestCountThreshold);
            Assert.AreEqual(CustomConfigSet.CircuitBreakerErrorThresholdPercentage, CommandComponents.ConfigSet.CircuitBreakerErrorThresholdPercentage);
            Assert.AreEqual(CustomConfigSet.FallbackMaxConcurrentCount, CommandComponents.ConfigSet.FallbackMaxConcurrentCount);
        }

        [TestMethod]
        public void SemaphoreIsolation_Key()
        {
            SL instance = new SL(TestCommandKey);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void SemaphoreIsolation_Key_GroupKey()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void SemaphoreIsolation_Key_GroupKey_Domain()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void SemaphoreIsolation_Key_GroupKey_Domain_Config()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain,
                configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void StartExecution_Success()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.Count);
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            instance.StartExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.Count);
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        public void StartExecution_RestartFailedSilently()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
            instance.StartExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            instance.StartExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        public void StartExecution_Rejected()
        {
            for (int i = 1; i <= DefaultConfigSet.CommandMaxConcurrentCount; i++)
            {
                SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
                instance.StartExecution();
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - i, TestSemaphore.CurrentCount);
            }

            try
            {
                SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
                instance.StartExecution();
                Assert.Fail("Execution should be rejected here.");
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.SemaphoreIsolationRejected, ex.FailureType);
            }
        }

        [TestMethod]
        public void EndExecution_ForStartedExecution()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
            instance.StartExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
            instance.EndExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        public void EndExecution_ForStartedExecution_UsingDispose()
        {
            using (SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain))
            {
                instance.StartExecution();
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
            }
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        public void EndExecution_ForNotStartedExecution()
        {
            SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain);
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            instance.EndExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            instance.EndExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);

            instance.StartExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
            instance.EndExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);

            instance.EndExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);

            using (instance = new SL(TestCommandKey))
            {
                instance.StartExecution();
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
            }
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);

            instance.EndExecution();
            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        public void MarkSuccess_RunOnce()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 1; i <= DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkSuccess();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.Success]);
                }
            }
        }

        [TestMethod]
        public void MarkSuccess_RunTwice()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 1; i <= DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkSuccess();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.Success]);

                    instance.MarkSuccess();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.Success]);
                }
            }
        }

        [TestMethod]
        public void MarkFailure_RunOnce()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 1; i <= DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkFailure();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.Failed]);
                }
            }
        }

        [TestMethod]
        public void MarkFailure_RunTwice()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 1; i <= DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkFailure();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.Failed]);

                    instance.MarkFailure();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.Failed]);
                }
            }
        }

        [TestMethod]
        public void MarkBadRequest_RunOnce()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 1; i <= DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkBadRequest();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);
                }
            }
        }

        [TestMethod]
        public void MarkBadRequest_RunTwice()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 1; i <= DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkBadRequest();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);

                    instance.MarkBadRequest();
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
                    Assert.AreEqual(i, counts[CommandExecutionEventEnum.BadRequest]);
                }
            }
        }

        [TestMethod]
        public void Execution_Success()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold * 2; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain))
                {
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    instance.MarkSuccess();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold * 2; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey, TestGroupKey, TestDomain))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    instance.MarkSuccess();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }
        }

        [TestMethod]
        public void Execution_ShortCircuited_ByFullFailure()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    instance.MarkFailure();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            try
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        instance.StartExecution();
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    finally
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    }
                }
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void Execution_ShortCircuited_ByHalfFailure()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    if (i % 2 == 0)
                        instance.MarkSuccess();
                    else
                        instance.MarkFailure();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            try
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        instance.StartExecution();
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    finally
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    }
                }
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void Execution_ShortCircuited_ByFullTimeout()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    Thread.Sleep(TimeoutInMilliseconds + 2);
                    instance.MarkSuccess();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            try
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        instance.StartExecution();
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    finally
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    }
                }
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void Execution_ShortCircuited_ByHalfTimeout()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    if (i % 2 == 0)
                        Thread.Sleep(TimeoutInMilliseconds + 2);
                    instance.MarkSuccess();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            try
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        instance.StartExecution();
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    finally
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    }
                }
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void Execution_ShortCircuited_ByFailureAndTimeout()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            CommandComponents.ConfigSet.CommandTimeoutInMilliseconds = TimeoutInMilliseconds;
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    if (i % 2 == 0)
                    {
                        if (i % 4 == 0)
                        {
                            instance.MarkFailure();
                            continue;
                        }

                        Thread.Sleep(TimeoutInMilliseconds + 2);
                    }

                    instance.MarkSuccess();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            try
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        instance.StartExecution();
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    finally
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    }
                }
            }
            catch (HystrixException ex)
            {
                Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
            }
        }

        [TestMethod]
        public void Execution_ShortCircuited_AutoRecover()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                    instance.MarkFailure();
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    using (SL instance = new SL(TestCommandKey))
                    {
                        ScenarioTestHelper.SleepHealthSnapshotInverval();
                        try
                        {
                            instance.StartExecution();
                            Assert.Fail("Short circuited exception should be thrown before.");
                        }
                        catch (HystrixException)
                        {
                            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                            throw;
                        }
                    }
                }
                catch (HystrixException ex)
                {
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                }
            }

            Thread.Sleep(DefaultConfigSet.CircuitBreakerSleepWindowInMilliseconds + 10);

            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    instance.StartExecution();
                    instance.MarkFailure();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    using (SL instance = new SL(TestCommandKey))
                    {
                        ScenarioTestHelper.SleepHealthSnapshotInverval();
                        try
                        {
                            instance.StartExecution();
                            Assert.Fail("Short circuited exception should be thrown before.");
                        }
                        catch (HystrixException)
                        {
                            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                            throw;
                        }
                    }
                }
                catch (HystrixException ex)
                {
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                }
            }

            Thread.Sleep(DefaultConfigSet.CircuitBreakerSleepWindowInMilliseconds + 10);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                using (SL instance = new SL(TestCommandKey))
                {
                    instance.StartExecution();
                    instance.MarkSuccess();
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                }
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }
        }

        [TestMethod]
        public void CreateInstance_Success()
        {
            Object instance = SL.CreateInstance(TestCommandKey);
            Assert.IsInstanceOfType(instance, typeof(SL));
            Assert.AreEqual(1, HystrixCommandBase.CommandComponentsCollection.Count);
            Assert.AreEqual(1, HystrixCommandBase.ExecutionSemaphores.Count);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
        }

        [TestMethod]
        public void CreateInstance_Fail()
        {
            foreach (string key in EmptyStrings)
            {
                try
                {
                    SL.CreateInstance(key);
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [TestMethod]
        public void StartExecution_WithInstance()
        {
            object instance = SL.CreateInstance(TestCommandKey);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            SL.StartExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void StartExecution_WithEmptyInstance()
        {
            SL.StartExecution(null);
        }

        [TestMethod]
        public void MarkSuccess_WithInstance()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            object instance = SL.CreateInstance(TestCommandKey);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            SL.StartExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            SL.MarkSuccess(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
            Assert.AreEqual(1, counts[CommandExecutionEventEnum.Success]);

            SL.EndExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MarkSuccess_WithEmptyInstance()
        {
            SL.MarkSuccess(null);
        }

        [TestMethod]
        public void MarkFailure_WithInstance()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            object instance = SL.CreateInstance(TestCommandKey);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            SL.StartExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            SL.MarkFailure(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
            Assert.AreEqual(1, counts[CommandExecutionEventEnum.Failed]);

            SL.EndExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MarkFailure_WithEmptyInstance()
        {
            SL.MarkFailure(null);
        }

        [TestMethod]
        public void MarkBadRequest_WithInstance()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            object instance = SL.CreateInstance(TestCommandKey);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            SL.StartExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            SL.MarkBadRequest(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);

            ScenarioTestHelper.SleepHealthSnapshotInverval();
            Dictionary<CommandExecutionEventEnum, int> counts = CommandComponents.Metrics.ToConcrete().GetExecutionEventDistribution();
            Assert.AreEqual(1, counts[CommandExecutionEventEnum.BadRequest]);

            SL.EndExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MarkBadRequest_WithEmptyInstance()
        {
            SL.MarkBadRequest(null);
        }

        [TestMethod]
        public void EndExecution_WithInstance()
        {
            object instance = SL.CreateInstance(TestCommandKey);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            SL.StartExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
            SL.EndExecution(instance);
            Assert.AreEqual(CommandComponents.ConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EndExecution_WithEmptyInstance()
        {
            SL.EndExecution(null);
        }

        [TestMethod]
        public void Execution_ShortCircuited_AutoRecover_WithInstance()
        {
            SL.Config(TestCommandKey, TestGroupKey, TestDomain);
            CommandComponents.ConfigSet.InitTestHealthSnapshotInterval();
            for (int i = 0; i < DefaultConfigSet.CircuitBreakerRequestCountThreshold; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                object instance = SL.CreateInstance(TestCommandKey);
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                SL.StartExecution(instance);
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                SL.MarkFailure(instance);
                SL.EndExecution(instance);
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    object instance = SL.CreateInstance(TestCommandKey);
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        SL.StartExecution(instance);
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    catch (HystrixException)
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                        throw;
                    }
                }
                catch (HystrixException ex)
                {
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                }
            }

            Thread.Sleep(DefaultConfigSet.CircuitBreakerSleepWindowInMilliseconds + 10);

            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                object instance = SL.CreateInstance(TestCommandKey);
                ScenarioTestHelper.SleepHealthSnapshotInverval();
                SL.StartExecution(instance);
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                SL.MarkFailure(instance);
                SL.EndExecution(instance);
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                    object instance = SL.CreateInstance(TestCommandKey);
                    ScenarioTestHelper.SleepHealthSnapshotInverval();
                    try
                    {
                        SL.StartExecution(instance);
                        Assert.Fail("Short circuited exception should be thrown before.");
                    }
                    catch (HystrixException)
                    {
                        Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                        throw;
                    }
                }
                catch (HystrixException ex)
                {
                    Assert.AreEqual(FailureTypeEnum.ShortCircuited, ex.FailureType);
                }
            }

            Thread.Sleep(DefaultConfigSet.CircuitBreakerSleepWindowInMilliseconds + 10);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                object instance = SL.CreateInstance(TestCommandKey);
                SL.StartExecution(instance);
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount - 1, TestSemaphore.CurrentCount);
                SL.MarkSuccess(instance);
                SL.EndExecution(instance);
                Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
            }
        }

        [TestMethod]
        public void Execution_SimulateReal()
        {
            CountdownEvent waitHandle = new CountdownEvent(DefaultConfigSet.CommandMaxConcurrentCount);
            for (int i = 0; i < DefaultConfigSet.CommandMaxConcurrentCount; i++)
            {
                Task.Factory.StartNew(
                    j =>
                    {
                        for (int k = 0; k < 10; k++)
                        {
                            using (SL instance = new SL(TestCommandKey))
                            {
                                instance.StartExecution();
                                Assert.AreNotEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                                Thread.Sleep(1000);
                                int v = (int)j;
                                if (v % 3 == 0)
                                    instance.MarkFailure();
                                else if (v % 5 == 0)
                                    instance.MarkBadRequest();
                                else
                                    instance.MarkSuccess();
                                Assert.AreNotEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                            }
                        }
                    }, i)
                    .ContinueWith(
                        t =>
                        {
                            var ex = t.Exception;
                            waitHandle.Signal();
                        });
            }

            bool success = waitHandle.Wait(180 * 1000);
            Assert.IsTrue(success);

            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }

        [TestMethod]
        public void Execution_SimulateReal_WithInstance()
        {
            CountdownEvent waitHandle = new CountdownEvent(DefaultConfigSet.CommandMaxConcurrentCount);
            for (int i = 0; i < DefaultConfigSet.CommandMaxConcurrentCount; i++)
            {
                Task.Factory.StartNew(
                    j =>
                    {
                        for (int k = 0; k < 10; k++)
                        {
                            object instance = SL.CreateInstance(TestCommandKey);
                            SL.StartExecution(instance);
                            Assert.AreNotEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                            Thread.Sleep(1000);
                            int v = (int)j;
                            if (v % 3 == 0)
                                SL.MarkFailure(instance);
                            else if (v % 5 == 0)
                                SL.MarkBadRequest(instance);
                            else
                                SL.MarkSuccess(instance);
                            Assert.AreNotEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
                            SL.EndExecution(instance);
                        }
                    }, i)
                    .ContinueWith(
                        t =>
                        {
                            var ex = t.Exception;
                            waitHandle.Signal();
                        });
            }

            bool success = waitHandle.Wait(180 * 1000);
            Assert.IsTrue(success);

            Assert.AreEqual(DefaultConfigSet.CommandMaxConcurrentCount, TestSemaphore.CurrentCount);
        }
    }
}
