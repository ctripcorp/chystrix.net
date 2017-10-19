using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CHystrix.Utils.Atomic;

namespace CHystrix.Utils
{
    internal class IsolationSemaphore
    {
        public int Count { get; set; }
        private AtomicInteger UsedCount;

        public IsolationSemaphore(int count)
        {
            Count = count;
            UsedCount = new AtomicInteger();
        }

        public int CurrentCount
        {
            get
            {
                return Count - UsedCount.Value;
            }
        }

        public bool TryAcquire()
        {
            int count = UsedCount.IncrementAndGet();
            if (count > Count)
            {
                UsedCount.DecrementAndGet();
                return false;
            }

            return true;
        }

        public void Release()
        {
            UsedCount.DecrementAndGet();
        }
    }
}
