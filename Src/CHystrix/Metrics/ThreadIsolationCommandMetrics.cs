using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CHystrix.Utils;

namespace CHystrix.Metrics
{
    internal class ThreadIsolationCommandMetrics : CommandMetrics
    {
        private readonly string Key;

        public ThreadIsolationCommandMetrics(ICommandConfigSet configSet, string key)
            : base(configSet)
        {
            Key = key;
        }

        public override int CurrentConcurrentExecutionCount
        {
            get
            {
                return CThreadPoolFactory.GetPoolByKey(Key).NowRunningWorkCount;
            }
        }

        public override int CurrentWaitCount
        {
            get
            {
                return CThreadPoolFactory.GetPoolByKey(Key).NowWaitingWorkCount;
            }
        }
    }
}
