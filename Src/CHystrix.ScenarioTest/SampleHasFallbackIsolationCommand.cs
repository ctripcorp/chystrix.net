using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace CHystrix.ScenarioTest
{
    public class SampleHasFallbackIsolationCommand : SampleIsolationCommand, IFallback<string>
    {
        public Func<string> FallbackDelegate { get; private set; }

        public SampleHasFallbackIsolationCommand(string commandKey, string groupKey = null, string domain = null,
            string expectedResult = null, int sleepTimeInMilliseconds = 0,
            Func<string> execute = null, Func<string> fallback = null, Action<ICommandConfigSet> config = null)
            : base(commandKey, groupKey, domain, expectedResult, sleepTimeInMilliseconds, execute, config)
        {
            if (fallback == null)
                fallback = () => ExpectedResult;
            FallbackDelegate = fallback;
        }

        public string GetFallback()
        {
            return FallbackDelegate();
        }
    }
}
