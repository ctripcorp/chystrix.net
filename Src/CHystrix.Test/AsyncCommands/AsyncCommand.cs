// -----------------------------------------------------------------------
// <copyright file="AsyncCommand.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test.Async
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AsyncCommand : ThreadIsolationCommand<string>, IFallback<string>
    {
        string _msg;
        public AsyncCommand(string msg)
        {
            ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
            Metrics.Reset();
            _msg = msg;
        }

        protected override string Execute()
        {
            return _msg;
        }

        public override string GroupKey
        {
            get { return "isolationTest"; }
        }

        public string GetFallback()
        {
            return "fallback";
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
