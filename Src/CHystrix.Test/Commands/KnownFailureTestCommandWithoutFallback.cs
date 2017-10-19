// -----------------------------------------------------------------------
// <copyright file="KnownFailureTestCommandWithoutFallback.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;


    /// <summary>
    /// Failed execution with known exception (HystrixException) - no fallback implementation.
    /// </summary>
    class KnownFailureTestCommandWithoutFallback : SemaphoreIsolationCommand<bool>
    {


        protected override bool Execute()
        {
            Console.WriteLine("*** simulated Assert.Failed execution *** ==> " + Thread.CurrentThread);
            throw new Exception("we Assert.Failed with a simulated issue");
        }

        public override string Domain
        {
            get { return "domain"; }
        }

        public override string GroupKey
        {
            get { return "groupkey"; }
        }
    }

}
