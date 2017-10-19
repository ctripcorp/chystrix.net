using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CHystrix.Utils;

namespace CHystrix.Metrics
{
    internal class SemaphoreIsolationCommandMetrics : CommandMetrics
    {
        private readonly string Key;

        public SemaphoreIsolationCommandMetrics(ICommandConfigSet configSet, string key)
            : base(configSet)
        {
            Key = key;
        }

        public override int CurrentConcurrentExecutionCount
        {
            get
            {
                IsolationSemaphore semaphore;
                HystrixCommandBase.ExecutionSemaphores.TryGetValue(Key, out semaphore);
                if (semaphore == null)
                    return 0;
                return semaphore.Count - semaphore.CurrentCount;
            }
        }

        public override int CurrentWaitCount
        {
            get
            {
                return 0;
            }
        }
    }
}
