using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal class CommandExecutionHealthSnapshot
    {
        public int TotalCount { get; private set; }
        public int ErrorPercentage { get; private set; }

        public CommandExecutionHealthSnapshot(int totalCount, int failedCount)
        {
            TotalCount = totalCount;
            ErrorPercentage = totalCount == 0 ? 0 : (int)Math.Floor(((double)failedCount / totalCount) * 100);
        }
    }
}
