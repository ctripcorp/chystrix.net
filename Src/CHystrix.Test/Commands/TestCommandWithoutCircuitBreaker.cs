// -----------------------------------------------------------------------
// <copyright file="TestCommandWithoutCircuitBreaker.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class TestCommandWithoutCircuitBreaker:SemaphoreIsolationCommand<bool>
    {

        public TestCommandWithoutCircuitBreaker()
        {
            Metrics.Reset();
            ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
            ConfigSetForTest.CircuitBreakerEnabled = false;
        }

        protected override bool Execute()
        {
            Console.WriteLine("successfully executed");
            return true;
        }
    }
}
