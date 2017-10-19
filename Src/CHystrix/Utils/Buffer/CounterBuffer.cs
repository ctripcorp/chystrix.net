using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using CHystrix.Utils.Atomic;

namespace CHystrix.Utils.Buffer
{
    internal class CounterBuffer<T> : CircularBuffer<CounterBucket<T>>
    {
        public CounterBuffer(int timeWindowInMilliseconds, int bucketCount)
            : base(timeWindowInMilliseconds, bucketCount)
        {
        }

        protected override CounterBucket<T> CreateEmptyBucket(long timeInMilliseconds)
        {
            return new CounterBucket<T>(timeInMilliseconds);
        }

        public int GetCount(T identity)
        {
            long currentBufferStartTime = GetCurrentBucketStartTimeInMilliseconds();
            int count = 0;
            for (int i = 0; i < Buckets.Length; i++)
            {
                CounterBucket<T> bucket = Buckets[i];
                if (currentBufferStartTime - bucket.TimeInMilliseconds < TimeWindowInMilliseconds)
                    count += bucket[identity];
            }
            return count;
        }

        public void IncreaseCount(T identity)
        {
            CurrentBucket.IncreaseCount(identity);
        }
    }
}
