// -----------------------------------------------------------------------
// <copyright file="Class1.cs" company="Microsoft">
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
    /// This should timeout.
    /// </summary>
    class TestCommandWithTimeout : SemaphoreIsolationCommand<bool>, IFallback<bool>
    {

        internal readonly int timeout;

        internal const int FALLBACK_NOT_IMPLEMENTED = 1;
        internal const int FALLBACK_SUCCESS = 2;
        internal const int FALLBACK_FAILURE = 3;

        internal readonly int fallbackBehavior;

        internal TestCommandWithTimeout(int timeout, int fallbackBehavior)
        {
            ConfigSet.CommandTimeoutInMilliseconds = timeout;
            this.timeout = timeout;
            this.fallbackBehavior = fallbackBehavior;
        }


        protected override bool Execute()
        {

            Console.WriteLine("***** running");
            try
            {
                Thread.Sleep(timeout * 10);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                // ignore and sleep some more to simulate a dependency that doesn't obey interrupts
                try
                {
                    Thread.Sleep(timeout * 2);
                }
                catch (Exception)
                {
                    // ignore
                }
                Console.WriteLine("after interruption with extra sleep");
            }
            return true;
        }

        public bool GetFallback()
        {
            if (fallbackBehavior == FALLBACK_SUCCESS)
            {
                return false;
            }
            else if (fallbackBehavior == FALLBACK_FAILURE)
            {
                throw new Exception("Assert.Failed on fallback");
            }
            else
            {
                // FALLBACK_NOT_IMPLEMENTED
                return true;
            }
        }
    }

}
