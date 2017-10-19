using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using CHystrix.Utils;

namespace CHystrix.Metrics
{
    internal static class MetricsReporter
    {
        public const int SendMetricsIntervalMilliseconds = 60 * 1000;

        const string HystrixEventDistributionMetricsName = "chystrix.execution.event.distribution";
        const string HystrixErrorPercentageMetricsName = "chystrix.error.percentage";
        const string HystrixExecutionLatencyPercentileMetricsName = "chystrix.execution.latency.percentile";
        const string HystrixTotalExecutionLatencyPercentileMetricsName = "chystrix.execution.total_latency.percentile";
        const string HystrixExecutionConcurrentCountMetricsName = "chystrix.execution.concurrent_count";
        const string HystrixResourceUtilizationMetricsName = "chystrix.resource.utilization";
        const string HystrixAppInstanceMetricsName = "chystrix.app.instance";

        static Dictionary<double, string> LatencyPercentileValues = new Dictionary<double, string>()
        {
            { 0, "0" },
            { 25, "25" },
            { 50, "50" },
            { 75, "75" },
            { 90, "90" },
            { 95, "95" },
            { 99, "99" },
            { 99.5, "99.5" },
            { 100, "100" }
        };

        private static Timer Timer;

        public static void Start()
        {
            if (Timer != null)
                return;

            Timer = new Timer()
            {
                Interval = SendMetricsIntervalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            Timer.Elapsed += new ElapsedEventHandler(SendMetrics);
        }

        public static void Reset()
        {
            try
            {
                if (Timer == null)
                    return;

                Timer timer = Timer;
                Timer = null;
                using (timer)
                {
                    timer.Stop();
                }
            }
            catch
            {
            }
        }

        private static void SendMetrics(object sender, ElapsedEventArgs arg)
        {
            try
            {
                Report();
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to send metrics.", ex, new Dictionary<string, string>().AddLogTagData("FXD303015"));
            }
        }

        private static void Report()
        {
            var tagMap = new Dictionary<string, string>();
            tagMap["app"] = HystrixCommandBase.HystrixAppName.ToLower();
            tagMap["version"] = HystrixCommandBase.HystrixVersion;
            string tag = string.Empty;
            var now = DateTime.Now;

            ; // log metrics

            Dictionary<string, CommandComponents> commandComponentsCollection = HystrixCommandBase.CommandComponentsCollection.ToDictionary(p => p.Key, p => p.Value);
            foreach (CommandComponents item in commandComponentsCollection.Values)
            {
                tagMap["instancekey"] = item.CommandInfo.InstanceKey;
                tagMap["commandkey"] = item.CommandInfo.CommandKey;
                tagMap["groupkey"] = item.CommandInfo.GroupKey;
                tagMap["domain"] = item.CommandInfo.Domain;
                tagMap["isolationmode"] = item.CommandInfo.Type;

                tag = "event";
                Dictionary<CommandExecutionEventEnum, int> eventDistribution = item.Metrics.GetExecutionEventDistribution();
                foreach (KeyValuePair<CommandExecutionEventEnum, int> item2 in eventDistribution)
                {
                    if (item2.Value <= 0)
                        continue;

                    if (item2.Key == CommandExecutionEventEnum.ExceptionThrown)
                        continue;

                    tagMap[tag] = item2.Key.ToString();
                    // log metrics
                }
                if (tagMap.ContainsKey(tag))
                    tagMap.Remove(tag);

                CommandExecutionHealthSnapshot healthSnapshot = item.Metrics.GetExecutionHealthSnapshot();
                if (healthSnapshot.ErrorPercentage > 0)
                    ; // log metrics

                tag = "percentile";
                foreach (KeyValuePair<double, string> item2 in LatencyPercentileValues)
                {
                    tagMap[tag] = item2.Value;
                    long latencyPencentile = item.Metrics.GetTotalExecutionLatencyPencentile(item2.Key);
                    if (latencyPencentile > 0)
                        ; // log metrics

                    if (item.IsolationMode == IsolationModeEnum.ThreadIsolation)
                    {
                        latencyPencentile = item.Metrics.GetExecutionLatencyPencentile(item2.Key);
                        if (latencyPencentile > 0)
                            ; // log metrics
                    }
                }
                if (tagMap.ContainsKey(tag))
                    tagMap.Remove(tag);

                int currentConcurrentExecutionCount = item.Metrics.CurrentConcurrentExecutionCount;
                if (currentConcurrentExecutionCount > 0)
                    ; // log metrics

                int utilization = 0;
                int resourceLimit = item.ConfigSet.CommandMaxConcurrentCount;
                if (resourceLimit > 0)
                    utilization = (int)((double)currentConcurrentExecutionCount / resourceLimit * 100);
                else
                    utilization = item.Metrics.CurrentConcurrentExecutionCount * 100;
                if (utilization > 0)
                    ; // log metrics
            }
        }
    }
}
