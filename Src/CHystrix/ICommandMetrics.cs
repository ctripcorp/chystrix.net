using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal interface ICommandMetrics
    {
        void MarkExecutionEvent(CommandExecutionEventEnum executionEvent);
        void MarkExecutionLatency(long milliseconds);
        void MarkTotalExecutionLatency(long milliseconds);

        Dictionary<CommandExecutionEventEnum, int> GetExecutionEventDistribution();
        CommandExecutionHealthSnapshot GetExecutionHealthSnapshot();

        long GetExecutionLatencyPencentile(double percentage);
        long GetTotalExecutionLatencyPencentile(double percentage);

        void GetExecutionLatencyAuditData(out int count, out long sum, out long min, out long max);
        void GetTotalExecutionLatencyAuditData(out int count, out long sum, out long min, out long max);

        long GetAverageExecutionLatency();
        long GetAverageTotalExecutionLatency();

        void Reset();

        int CurrentConcurrentExecutionCount { get; }

        int CurrentWaitCount { get; }


    }
}
