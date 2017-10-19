using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using CHystrix.Utils;
using CHystrix.Utils.Buffer;

namespace CHystrix.Metrics
{
    internal abstract class CommandMetrics : ICommandMetrics
    {
        protected readonly ICommandConfigSet ConfigSet;

        protected CounterBuffer<CommandExecutionEventEnum> ExecutionEventBuffer;
        protected IntegerPercentileBuffer ExecutionLatencyBuffer;
        protected IntegerPercentileBuffer TotalExecutionLatencyBuffer;

        protected long _lastUpdateExecutionEventSnapshotTimeInMilliseconds;
        protected Dictionary<CommandExecutionEventEnum, int> _executionEventDistributionSnapshot;
        protected CommandExecutionHealthSnapshot _executionEventHealthSnapshot;

        protected long _lastGetLatencyBufferSnapshotTimeInMilliseconds;
        protected List<long> _latencyBufferSnapshot;

        protected long _lastGetTotalLatencyBufferSnapshotTimeInMilliseconds;
        protected List<long> _totalLatencyBufferSnapshot;

        public CommandMetrics(ICommandConfigSet configSet)
        {
            ConfigSet = configSet;
            Reset();
        }

        public void MarkExecutionEvent(CommandExecutionEventEnum executionEvent)
        {
            ExecutionEventBuffer.IncreaseCount(executionEvent);
        }

        public void MarkExecutionLatency(long milliseconds)
        {
            ExecutionLatencyBuffer.Add(milliseconds);
        }

        public void MarkTotalExecutionLatency(long milliseconds)
        {
            TotalExecutionLatencyBuffer.Add(milliseconds);
        }

        public Dictionary<CommandExecutionEventEnum, int> GetExecutionEventDistribution()
        {
            UpdateExecutionEventSnapshot();
            return _executionEventDistributionSnapshot;
        }

        public CommandExecutionHealthSnapshot GetExecutionHealthSnapshot()
        {
            UpdateExecutionEventSnapshot();
            return _executionEventHealthSnapshot;
        }

        public long GetExecutionLatencyPencentile(double percentage)
        {
            UpdateExecutionLatencyBufferSnapshot();
            return _latencyBufferSnapshot.GetPercentile(percentage, true);
        }

        public long GetTotalExecutionLatencyPencentile(double percentage)
        {
            UpdateTotalExecutionLatencyBufferSnapshot();
            return _totalLatencyBufferSnapshot.GetPercentile(percentage, true);
        }

        public void GetExecutionLatencyAuditData(out int count, out long sum, out long min, out long max)
        {
            UpdateExecutionLatencyBufferSnapshot();
            _latencyBufferSnapshot.GetAuditData(out count, out sum, out min, out max);
        }

        public void GetTotalExecutionLatencyAuditData(out int count, out long sum, out long min, out long max)
        {
            UpdateTotalExecutionLatencyBufferSnapshot();
            _totalLatencyBufferSnapshot.GetAuditData(out count, out sum, out min, out max);
        }

        public long GetAverageExecutionLatency()
        {
            UpdateExecutionLatencyBufferSnapshot();
            List<long> snapshot = _latencyBufferSnapshot;
            if (snapshot.Count == 0)
                return 0;
            return (long)snapshot.Average();
        }

        public long GetAverageTotalExecutionLatency()
        {
            UpdateTotalExecutionLatencyBufferSnapshot();
            List<long> snapshot = _totalLatencyBufferSnapshot;
            if (snapshot.Count == 0)
                return 0;
            return (long)snapshot.Average();
        }

        public void Reset()
        {
            ExecutionEventBuffer = new CounterBuffer<CommandExecutionEventEnum>(
                ConfigSet.MetricsRollingStatisticalWindowInMilliseconds, ConfigSet.MetricsRollingStatisticalWindowBuckets);
            ExecutionLatencyBuffer = new IntegerPercentileBuffer(
                ConfigSet.MetricsRollingPercentileWindowInMilliseconds,
                ConfigSet.MetricsRollingPercentileWindowBuckets,
                ConfigSet.MetricsRollingPercentileBucketSize);
            TotalExecutionLatencyBuffer = new IntegerPercentileBuffer(
                ConfigSet.MetricsRollingPercentileWindowInMilliseconds,
                ConfigSet.MetricsRollingPercentileWindowBuckets,
                ConfigSet.MetricsRollingPercentileBucketSize);

            _lastUpdateExecutionEventSnapshotTimeInMilliseconds = 0;
            _executionEventDistributionSnapshot = new Dictionary<CommandExecutionEventEnum,int>();
            _executionEventHealthSnapshot = new CommandExecutionHealthSnapshot(0, 0);

            _lastGetLatencyBufferSnapshotTimeInMilliseconds = 0;
            _latencyBufferSnapshot = new List<long>();

            _lastGetTotalLatencyBufferSnapshotTimeInMilliseconds = 0;
            _totalLatencyBufferSnapshot = new List<long>();
        }

        private void UpdateExecutionEventSnapshot()
        {
            long currentTimeInMilliseconds = CommonUtils.CurrentTimeInMiliseconds;
            if (_lastUpdateExecutionEventSnapshotTimeInMilliseconds == 0
                || _lastUpdateExecutionEventSnapshotTimeInMilliseconds + ConfigSet.MetricsHealthSnapshotIntervalInMilliseconds <= currentTimeInMilliseconds)
            {
                Dictionary<CommandExecutionEventEnum, int> executionEventDistribution = new Dictionary<CommandExecutionEventEnum, int>();
                foreach (CommandExecutionEventEnum @event in CommonUtils.CommandExecutionEvents)
                {
                    executionEventDistribution[@event] = ExecutionEventBuffer.GetCount(@event);
                }

                _executionEventHealthSnapshot = executionEventDistribution.GetHealthSnapshot();
                _executionEventDistributionSnapshot = executionEventDistribution;
                _lastUpdateExecutionEventSnapshotTimeInMilliseconds = currentTimeInMilliseconds;
            }
        }

        private void UpdateExecutionLatencyBufferSnapshot()
        {
            long currentTimeInMilliseconds = CommonUtils.CurrentTimeInMiliseconds;
            if (_lastGetLatencyBufferSnapshotTimeInMilliseconds == 0
                || _lastGetLatencyBufferSnapshotTimeInMilliseconds + ConfigSet.MetricsHealthSnapshotIntervalInMilliseconds <= currentTimeInMilliseconds)
            {
                List<long> snapshot = ExecutionLatencyBuffer.GetSnapShot();
                snapshot.Sort();
                _latencyBufferSnapshot = snapshot;
                _lastGetLatencyBufferSnapshotTimeInMilliseconds = currentTimeInMilliseconds;
            }
        }

        private void UpdateTotalExecutionLatencyBufferSnapshot()
        {
            long currentTimeInMilliseconds = CommonUtils.CurrentTimeInMiliseconds;
            if (_lastGetTotalLatencyBufferSnapshotTimeInMilliseconds == 0
                || _lastGetTotalLatencyBufferSnapshotTimeInMilliseconds + ConfigSet.MetricsHealthSnapshotIntervalInMilliseconds <= currentTimeInMilliseconds)
            {
                List<long> snapshot = TotalExecutionLatencyBuffer.GetSnapShot();
                snapshot.Sort();
                _totalLatencyBufferSnapshot = snapshot;
                _lastGetTotalLatencyBufferSnapshotTimeInMilliseconds = currentTimeInMilliseconds;
            }
        }

        public abstract int CurrentConcurrentExecutionCount { get; }

        public abstract int CurrentWaitCount { get; }
    }
}
