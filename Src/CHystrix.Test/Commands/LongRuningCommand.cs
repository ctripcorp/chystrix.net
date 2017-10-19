// -----------------------------------------------------------------------
// <copyright file="LongRuningCommand.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Diagnostics;
    using System.Threading;


    public class LongRuningCommand : SemaphoreIsolationCommand<string>, IFallback<string>
    {
        private int _waitTime = -1;

        string _key = "isolationTest";
        public LongRuningCommand(int runTime)
        {
            _waitTime = runTime;
        }

        public LongRuningCommand(int runTime, string key)
        {
            _waitTime = runTime;
            _key = key;
        }

        protected override string Execute()
        {
            Trace.WriteLine("start LongRuningCommand");
            Thread.Sleep(_waitTime);
            Trace.WriteLine("finish LongRuningCommand");
            return "command execute complete";
        }

        public override string GroupKey
        {
            get { return _key; }
        }

        public string GetFallback()
        {
            return "command timeout";
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
