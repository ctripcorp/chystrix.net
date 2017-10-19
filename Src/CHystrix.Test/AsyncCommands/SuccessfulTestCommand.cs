// -----------------------------------------------------------------------
// <copyright file="SuccessfulTestCommand.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test.Async
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    public class SuccessfulTestCommand : ThreadIsolationCommand<bool>, IFallback<bool>
    {
        public SuccessfulTestCommand()
        {
            this.ConfigSet.CircuitBreakerForceClosed = true;
            this.ConfigSet.CircuitBreakerForceOpen = false;
            
            ConfigSetForTest.CommandTimeoutInMilliseconds = 50000;
            ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
            Metrics.Reset();

        }

        protected override bool Execute()
        {
            Thread.Sleep(10);
            return true;
        }

        public override string GroupKey
        {
            get { return "isolationTest"; }
        }

        public bool GetFallback()
        {
            return false;
        }

        public bool Run()
        {
            return true;
        }

        public override string Domain
        {
            get { return "domain"; }
        }

        internal override IsolationModeEnum IsolationMode
        {
            get { return IsolationModeEnum.ThreadIsolation; }
        }
    }
}
