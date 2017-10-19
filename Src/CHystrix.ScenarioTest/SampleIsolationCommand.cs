using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace CHystrix.ScenarioTest
{
    public class SampleIsolationCommand : SemaphoreIsolationCommand<string>
    {
        public string ExpectedResult { get; private set; }
        public int SleepTimeInMilliseconds { get; private set; }

        public Func<string> ExecuteDelegate { get; private set; }

        public SampleIsolationCommand(string commandKey, string groupKey = null, string domain = null, string expectedResult = null,
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
    }
}
