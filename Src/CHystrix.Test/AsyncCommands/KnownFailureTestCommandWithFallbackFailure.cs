// -----------------------------------------------------------------------
// <copyright file="KnownFailureTestCommandWithFallbackFailure.cs" company="Microsoft">
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
    /// Failed execution - fallback implementation throws exception.
    /// </summary>
    class KnownFailureTestCommandWithFallbackFailure : ThreadIsolationCommand<bool>, IFallback<bool>
    {

        public KnownFailureTestCommandWithFallbackFailure()
        {
            Metrics.Reset();
            ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
        }

        protected override bool Execute()
        {
            throw new Exception("KnownFailureTestCommandWithFallback failed");
        }

        public override string GroupKey
        {
            get { return "isolationTest"; }
        }

        public bool GetFallback()
        {
            throw new Exception("fallback failed");
        }

        public override string Domain
        {
            get { return "domain"; }
        }
    }
}
