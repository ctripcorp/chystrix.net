// -----------------------------------------------------------------------
// <copyright file="Class1.cs" company="Microsoft">
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
    /// Failed execution - fallback implementation successfully returns value.
    /// </summary>
    class KnownFailureTestCommandWithFallback : ThreadIsolationCommand<bool>, IFallback<bool>
    {
        public KnownFailureTestCommandWithFallback()
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
            return false;
        }

        public override string Domain
        {
            get { return "domain"; }
        }
    }
}
