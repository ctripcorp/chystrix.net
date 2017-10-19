using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.Config
{
    [TestClass]
    public class ConfigOverriddenTests : TestClassBase
    {
        protected override void  CustomTestCleanup()
        {
            SampleIsolationCommandWithOverriddenConfig.Reset();
        }

        [TestMethod]
        public void OverrideConfigInSubclass_WithoutCustomConfig()
        {
            SampleIsolationCommandWithOverriddenConfig command = new SampleIsolationCommandWithOverriddenConfig(TestCommandKey);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void OverrideConfigInSubclass_WithCustomConfig_RunInBase()
        {
            SampleIsolationCommandWithOverriddenConfig command = new SampleIsolationCommandWithOverriddenConfig(TestCommandKey,
                config: configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void OverrideConfigInSubclass_WithCustomConfig_RunInConcrete()
        {
            SampleIsolationCommandWithOverriddenConfig.InitConfigSet = CustomConfigSet;
            SampleIsolationCommandWithOverriddenConfig command = new SampleIsolationCommandWithOverriddenConfig(TestCommandKey);
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void OverrideConfigInSubclass_WithCustomConfig_ConcreteFirst()
        {
            SampleIsolationCommandWithOverriddenConfig.InitConfigSet = DefaultConfigSet;
            SampleIsolationCommandWithOverriddenConfig command = new SampleIsolationCommandWithOverriddenConfig(TestCommandKey,
                config: configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, CustomConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(DefaultConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void OverrideConfigInSubclass_WithCustomConfig_ConcreteFirst2()
        {
            SampleIsolationCommandWithOverriddenConfig.InitConfigSet = CustomConfigSet;
            SampleIsolationCommandWithOverriddenConfig command = new SampleIsolationCommandWithOverriddenConfig(TestCommandKey,
                config: configSet => ScenarioTestHelper.SetCommandConfigFrom(configSet, DefaultConfigSet));
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }

        [TestMethod]
        public void OverrideConfigInSubclass_WithCustomConfig_ConcreteFirst3()
        {
            SampleIsolationCommandWithOverriddenConfig.InitConfigSet = CustomConfigSet;
            SampleIsolationCommandWithOverriddenConfig command = new SampleIsolationCommandWithOverriddenConfig(TestCommandKey,
                config: configSet => { configSet.CommandTimeoutInMilliseconds = 111; });
            Assert.AreEqual(TestCommandKey, CommandComponents.CommandInfo.Key, true);
            Assert.AreEqual(HystrixCommandBase.DefaultGroupKey, CommandComponents.CommandInfo.GroupKey, true);
            Assert.AreEqual(CommandDomains.Default, CommandComponents.CommandInfo.Domain, true);
            Assert.IsTrue(ScenarioTestHelper.AreEqual(CustomConfigSet, CommandComponents.ConfigSet));
        }
    }
}
