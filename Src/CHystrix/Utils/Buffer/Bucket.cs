using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix.Utils.Buffer
{
    internal abstract class Bucket
    {
        public long TimeInMilliseconds { get; protected set; }

        protected Bucket(long timeInMilliseconds)
        {
            TimeInMilliseconds = timeInMilliseconds;
        }
    }
}
