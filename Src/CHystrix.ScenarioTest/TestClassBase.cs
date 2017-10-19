using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CHystrix.Utils;

namespace CHystrix.ScenarioTest
{
    [TestClass]
    public class TestClassBase
    {
        protected const string DefaultTestCommandKey = "Default-Test-Command";
        protected const string DefaultTestGroupKey = "Default-Test-Group";
        protected const string DefaultTestDomain = "Default-Test-Domain";

        protected static readonly List<string> EmptyStrings = new List<string>()
        {
            null, string.Empty, "\n", "\r", "\t", " "
        };

        protected ICommandConfigSet DefaultConfigSet = ComponentFactory.CreateCommandConfigSet(IsolationModeEnum.SemaphoreIsolation);
        protected ICommandConfigSet CustomConfigSet = ScenarioTestHelper.CreateCustomConfigSet(IsolationModeEnum.SemaphoreIsolation);

        protected string Expected
        {
            get { return string.Empty; }
        }

        protected string Fallback
        {
            get { return null; }
        }

        protected int TimeoutInMilliseconds
        {
            get { return 5; }
        }

        protected virtual string TestCommandKey
        {
            get
            {
                return DefaultTestCommandKey;
            }
        }

        protected virtual string TestGroupKey
        {
            get
            {
                return DefaultTestGroupKey;
            }
        }

        protected virtual string TestDomain
        {
            get
            {
                return DefaultTestDomain;
            }
        }

        internal IsolationSemaphore TestSemaphore
        {
            get
            {
                IsolationSemaphore semaphore;
                HystrixCommandBase.ExecutionSemaphores.TryGetValue(TestCommandKey, out semaphore);
                return semaphore;
            }
        }

        protected virtual void CustomTestInit()
        {
        }

        protected virtual void CustomTestCleanup()
        {
        }

        internal CommandComponents CommandComponents
        {
            get
            {
                return HystrixCommandBase.CommandComponentsCollection[TestCommandKey];
            }
        }

        [TestInitialize]
        public void TestInit()
        {
            try
            {
                CustomTestInit();
            }
            catch
            {
                TestCleanup();
                throw;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            HystrixCommandBase.Reset();
            CustomTestCleanup();
        }
    }
}
