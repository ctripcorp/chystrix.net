// -----------------------------------------------------------------------
// <copyright file="FaultRuningCommand.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test.Async
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;


    public class FaultRuningCommand : ThreadIsolationCommand<string>, IFallback<string>
    {
        public FaultRuningCommand()
        {
            Metrics.Reset();
            ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
        }


        protected override string Execute()
        {
            throw (new Exception("wrong command"));
        }

        public override string GroupKey
        {
            get { return "isolationTest"; }
        }

        public string GetFallback()
        {
            return "command failed";
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
