using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace CHystrix.ScenarioTest
{
    public class SampleIsolationCommandWithOverriddenConfig : SemaphoreIsolationCommand<string>
    {
        public string ExpectedResult { get; private set; }
        public int SleepTimeInMilliseconds { get; private set; }

        public static ICommandConfigSet InitConfigSet { get; set; }

        public static void Reset()
        {
            InitConfigSet = null;
        }

        public Func<string> ExecuteDelegate { get; private set; }

        public SampleIsolationCommandWithOverriddenConfig(string commandKey, string groupKey = null, string domain = null, string expectedResult = null,
            int sleepTimeInMilliseconds = 0, Func<string> execute = null, Action<ICommandConfigSet> config = null)
            : base(commandKey, groupKey, domain, config, false)
        {
            ExpectedResult = expectedResult;
            SleepTimeInMilliseconds = sleepTimeInMilliseconds;

            if (execute == null)
                execute = () => ExpectedResult;
            ExecuteDelegate = execute;
        }

        protected override string Execute()
        {
            if (SleepTimeInMilliseconds > 0)
                Thread.Sleep(SleepTimeInMilliseconds);

            return ExecuteDelegate();
        }

        protected override void Config(ICommandConfigSet configSet)
        {
            if (InitConfigSet == null)
            {
                base.Config(configSet);
                return;
            }

            ScenarioTestHelper.SetCommandConfigFrom(configSet, InitConfigSet);
        }
    }
}
