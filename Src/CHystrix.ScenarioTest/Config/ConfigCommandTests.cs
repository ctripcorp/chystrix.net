using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.Config
{
    [TestClass]
    public class ConfigCommandTests : TestClassBase
    {
        [TestMethod]
        public void RunCommandWithDefaultConfig()
        {
            HystrixCommandBase.RunCommand<string>(TestCommandKey, () => string.Empty);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void RunCommandWithDefaultConfigFromConcreteClass()
        {
            new SampleIsolationCommand(TestCommandKey).Run();
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigCommandWithDefaultConfig()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, config => {});
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigCommandWithDefaultConfigFromConcreteClass()
        {
            new SampleIsolationCommand(TestCommandKey, config: c => { });
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigAllCommandSettings()
        {
            Assert.IsFalse(ScenarioTestHelper.AreEqual(DefaultConfigSet, CustomConfigSet));
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey,
                configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigAllCommandSettingsFromConcreteClass()
        {
            Assert.IsFalse(ScenarioTestHelper.AreEqual(DefaultConfigSet, CustomConfigSet));
            new SampleIsolationCommand(TestCommandKey,
                config: configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigCommand_Key_Domain()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestDomain);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
        }

        [TestMethod]
        public void ConfigCommand_Key_Group_Domain()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestGroupKey, TestDomain);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
        }

        [TestMethod]
        public void ConfigCommand_Key_Group_Domain_MaxConcurrentCount()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestGroupKey, TestDomain, CustomConfigSet.CommandMaxConcurrentCount);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.AreEqual(CustomConfigSet.CommandMaxConcurrentCount, CommandComponents.ConfigSet.CommandMaxConcurrentCount);
        }

        [TestMethod]
        public void ConfigCommand_Key_Group_Domain_MaxConcurrentCount_Timeout()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestGroupKey, TestDomain, CustomConfigSet.CommandMaxConcurrentCount,
                CustomConfigSet.CommandTimeoutInMilliseconds);
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.AreEqual(CustomConfigSet.CommandMaxConcurrentCount, CommandComponents.ConfigSet.CommandMaxConcurrentCount);
            Assert.AreEqual(CustomConfigSet.CommandTimeoutInMilliseconds, CommandComponents.ConfigSet.CommandTimeoutInMilliseconds);
        }

        [TestMethod]
        public void ConfigCommand_Key_Domain_Config()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestDomain,
                config => ScenarioTestHelper.SetCommandConfigFrom(config, CustomConfigSet));
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigCommand_Key_Group_Domain_Config()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestGroupKey, TestDomain,
                config => ScenarioTestHelper.SetCommandConfigFrom(config, CustomConfigSet));
            Assert.AreEqual(TestGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(TestDomain, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void ConfigCommand_Key_Group_Domain_MaxConcurrentCount_Timeout_RequestCountThreshold_ErrorThresholdPercentage_FallbackMaxConcurrentCount()
        {
            HystrixCommandBase.ConfigCommand<string>(TestCommandKey, TestGroupKey, TestDomain,
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
    }
}
