﻿// -----------------------------------------------------------------------
// <copyright file="UnknownFailureTestCommandWithoutFallback.cs" company="Microsoft">
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
    /// Failed execution with unknown exception (not HystrixException) - no fallback implementation.
    /// </summary>
    class UnknownFailureTestCommandWithoutFallback : SemaphoreIsolationCommand<bool>
    {

        public override string Domain
        {
            get { return "domain"; }
        }

        public override string GroupKey
        {
            get { return "groupkey"; }
        }

        protected override bool Execute()
        {
            Console.WriteLine("*** simulated Assert.Failed execution ***");
            throw new Exception("we Assert.Failed with an unknown issue");
        }

    }
}
