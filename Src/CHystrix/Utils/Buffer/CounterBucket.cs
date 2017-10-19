using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using CHystrix.Utils.Atomic;

namespace CHystrix.Utils.Buffer
{
    internal class CounterBucket<T> : Bucket
    {
        protected ConcurrentDictionary<T, AtomicInteger> Counters { get; set; }

        public CounterBucket(long timeInMilliseconds)
            : base(timeInMilliseconds)
        {
            Counters = new ConcurrentDictionary<T, AtomicInteger>();
        }

        public int this[T identity]
        {
            get
            {
                AtomicInteger counter;
                Counters.TryGetValue(identity, out counter);
                return counter;
            }
        }

        public void IncreaseCount(T identity)
        {
            AtomicInteger counter = Counters.GetOrAdd(identity, id => new AtomicInteger());
            counter.IncrementAndGet();
        }
    }
}
