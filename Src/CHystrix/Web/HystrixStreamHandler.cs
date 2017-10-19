using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Threading;
using System.Runtime.Serialization;

using CHystrix.Utils;
using CHystrix.Utils.Web;
using CHystrix.Threading;
using CHystrix.Utils.Extensions;

namespace CHystrix.Web
{
    internal class HystrixStreamHandler : IHttpHandler, IRouteHandler
    {
        const int ClientRetryMilliseconds = 100;
        public const string OperationName = "_hystrix_stream";

        const string TurbineStrategySemaphore = "SEMAPHORE";
        const string TurbineStrategyThread = "THREAD";

        const string TurbineDataTypeHystrixCommand = "HystrixCommand";
        const string TurbineDataTypeThreadPool = "HystrixThreadPool"; 

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return this;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                bool isPreflight = context.EnableCrossDomainSupport();
                if (isPreflight)
                    return;

                context.Response.AddHeader(HttpHeaders.ContentType, HttpContentTypes.EventStream + HttpContentTypes.Utf8Suffix);
                context.Response.AddHeader(HttpHeaders.CacheControl, "no-cache, no-store, max-age=0, must-revalidate");
                context.Response.AddHeader("Pragma", "no-cache");

                context.Response.Write(string.Format("retry: {0}\n", ClientRetryMilliseconds));

                List<HystrixCommandInfo> commandInfoList = GetHystrixCommandInfoList();
                foreach (HystrixCommandInfo commandInfo in commandInfoList)
                {
                    context.Response.Write(string.Format("data: {0}\n\n", RefinePercentileString(commandInfo.ToJson())));
                    context.Response.Flush();
                }

                List<HystrixThreadPoolInfo> threadPoolInfoList = GetHystrixThreadPoolList();
                foreach (HystrixThreadPoolInfo threadPoolInfo in threadPoolInfoList)
                {
                    context.Response.Write(string.Format("data: {0}\n\n", threadPoolInfo.ToJson()));
                    context.Response.Flush();
                }
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to report Hystrix.stream metrics.", ex,
                    new Dictionary<string, string>().AddLogTagData("FXD303027"));
            }
        }

        public static List<HystrixCommandInfo> GetHystrixCommandInfoList()
        {
            List<HystrixCommandInfo> hystrixCommandInfoList = new List<HystrixCommandInfo>();
            CommandComponents[] commandComponentsList = HystrixCommandBase.CommandComponentsCollection.Values.ToArray();
            foreach (CommandComponents item in commandComponentsList)
            {
                CommandExecutionHealthSnapshot healthSnapshot = item.Metrics.GetExecutionHealthSnapshot();
                Dictionary<CommandExecutionEventEnum, int> commandExecutionEventDistribution = item.Metrics.GetExecutionEventDistribution();
                HystrixCommandInfo hystrixCommandInfo = new HystrixCommandInfo()
                {
                    type = TurbineDataTypeHystrixCommand,
                    name = item.CommandInfo.Key,
                    group = item.CommandInfo.InstanceKey == null ? item.CommandInfo.GroupKey : item.CommandInfo.CommandKey,
                    currentTime = CommonUtils.CurrentUnixTimeInMilliseconds,
                    isCircuitBreakerOpen = item.CircuitBreaker.IsOpen(),
                    errorPercentage = healthSnapshot.ErrorPercentage,
                    errorCount = commandExecutionEventDistribution.Where(p => CommonUtils.CoreFailedCommandExecutionEvents.Contains(p.Key)).Select(p => p.Value).Sum(),
                    requestCount = commandExecutionEventDistribution.Where(p => CommonUtils.CoreCommandExecutionEvents.Contains(p.Key)).Select(p => p.Value).Sum(),
                    rollingCountExceptionsThrown = commandExecutionEventDistribution[CommandExecutionEventEnum.ExceptionThrown],
                    rollingCountFailure = commandExecutionEventDistribution[CommandExecutionEventEnum.Failed],
                    rollingCountSemaphoreRejected = item.IsolationMode == IsolationModeEnum.SemaphoreIsolation ? commandExecutionEventDistribution[CommandExecutionEventEnum.Rejected] : 0,
                    rollingCountShortCircuited = commandExecutionEventDistribution[CommandExecutionEventEnum.ShortCircuited],
                    rollingCountSuccess = commandExecutionEventDistribution[CommandExecutionEventEnum.Success],
                    rollingCountThreadPoolRejected = item.IsolationMode == IsolationModeEnum.ThreadIsolation ? commandExecutionEventDistribution[CommandExecutionEventEnum.Rejected] : 0,
                    rollingCountTimeout = commandExecutionEventDistribution[CommandExecutionEventEnum.Timeout],
                    rollingCountFallbackFailure = commandExecutionEventDistribution[CommandExecutionEventEnum.FallbackFailed],
                    rollingCountFallbackSuccess = commandExecutionEventDistribution[CommandExecutionEventEnum.FallbackSuccess],
                    rollingCountFallbackRejection = commandExecutionEventDistribution[CommandExecutionEventEnum.FallbackRejected],
                    latencyExecute = new PercentileInfo()
                    {
                         P0 = item.Metrics.GetExecutionLatencyPencentile(0),
                         P25 = item.Metrics.GetExecutionLatencyPencentile(25),
                         P50 = item.Metrics.GetExecutionLatencyPencentile(50),
                         P75 = item.Metrics.GetExecutionLatencyPencentile(75),
                         P90 = item.Metrics.GetExecutionLatencyPencentile(90),
                         P95 = item.Metrics.GetExecutionLatencyPencentile(95),
                         P99 = item.Metrics.GetExecutionLatencyPencentile(99),
                         P99DOT5 = item.Metrics.GetExecutionLatencyPencentile(99.5),
                         P100 = item.Metrics.GetExecutionLatencyPencentile(100)
                    },
                    latencyExecute_mean = item.Metrics.GetAverageExecutionLatency(),
                    latencyTotal = new PercentileInfo()
                    {
                         P0 = item.Metrics.GetTotalExecutionLatencyPencentile(0),
                         P25 = item.Metrics.GetTotalExecutionLatencyPencentile(25),
                         P50 = item.Metrics.GetTotalExecutionLatencyPencentile(50),
                         P75 = item.Metrics.GetTotalExecutionLatencyPencentile(75),
                         P90 = item.Metrics.GetTotalExecutionLatencyPencentile(90),
                         P95 = item.Metrics.GetTotalExecutionLatencyPencentile(95),
                         P99 = item.Metrics.GetTotalExecutionLatencyPencentile(99),
                         P99DOT5 = item.Metrics.GetTotalExecutionLatencyPencentile(99.5),
                         P100 = item.Metrics.GetTotalExecutionLatencyPencentile(100)
                    },
                    latencyTotal_mean = item.Metrics.GetAverageTotalExecutionLatency(),
                    reportingHosts = 1,
                    propertyValue_circuitBreakerEnabled = item.ConfigSet.CircuitBreakerEnabled,
                    propertyValue_circuitBreakerErrorThresholdPercentage = item.ConfigSet.CircuitBreakerErrorThresholdPercentage,
                    propertyValue_circuitBreakerForceClosed = item.ConfigSet.CircuitBreakerForceClosed,
                    propertyValue_circuitBreakerForceOpen = item.ConfigSet.CircuitBreakerForceOpen,
                    propertyValue_circuitBreakerRequestVolumeThreshold = item.ConfigSet.CircuitBreakerRequestCountThreshold,
                    propertyValue_circuitBreakerSleepWindowInMilliseconds = item.ConfigSet.CircuitBreakerSleepWindowInMilliseconds,
                    propertyValue_executionIsolationSemaphoreMaxConcurrentRequests = item.ConfigSet.CommandMaxConcurrentCount,
                    propertyValue_executionIsolationStrategy = item.IsolationMode == IsolationModeEnum.SemaphoreIsolation ? TurbineStrategySemaphore : TurbineStrategyThread,
                    propertyValue_executionIsolationThreadTimeoutInMilliseconds = item.ConfigSet.CommandTimeoutInMilliseconds,
                    propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests = item.ConfigSet.FallbackMaxConcurrentCount,
                    propertyValue_metricsRollingStatisticalWindowInMilliseconds = item.ConfigSet.MetricsRollingStatisticalWindowInMilliseconds,
                    currentConcurrentExecutionCount = item.Metrics.CurrentConcurrentExecutionCount,
                };

                hystrixCommandInfoList.Add(hystrixCommandInfo);
            }

            return hystrixCommandInfoList;
        }

        public static List<HystrixThreadPoolInfo> GetHystrixThreadPoolList()
        {
            List<HystrixThreadPoolInfo> hystrixThreadPoolList = new List<HystrixThreadPoolInfo>();

            var allkeys = CThreadPoolFactory.AllPools.Keys.ToArray();

            foreach (var key in allkeys)
            {
                CThreadPool pool;
                if (CThreadPoolFactory.AllPools.TryGetValue(key, out pool))
                {

                    CommandComponents commandComponent;

                    if (HystrixCommandBase.CommandComponentsCollection.TryGetValue(key, out commandComponent))
                    {
                        var config = commandComponent.ConfigSet;
                        hystrixThreadPoolList.Add(new HystrixThreadPoolInfo
                        {
                            type = TurbineDataTypeThreadPool,
                            name = key,
                            currentActiveCount = pool.NowRunningWorkCount,
                            currentCorePoolSize = pool.PoolThreadCount,
                            currentQueueSize = pool.NowWaitingWorkCount,
                            currentLargestPoolSize = pool.LargestPoolSize,
                            currentMaximumPoolSize = pool.MaxConcurrentCount,
                            currentCompletedTaskCount = pool.FinishedWorkCount,
                            currentPoolSize = pool.CurrentPoolSize,
                            currentTaskCount = pool.CurrentTaskCount,
                            currentTime = CommonUtils.CurrentUnixTimeInMilliseconds,
                            propertyValue_metricsRollingStatisticalWindowInMilliseconds =
                            config.MetricsRollingStatisticalWindowInMilliseconds,
                            propertyValue_queueSizeRejectionThreshold =
                            config.CommandMaxConcurrentCount * config.MaxAsyncCommandExceedPercentage / 100,
                            reportingHosts = 1,
                            rollingCountThreadsExecuted = 0,
                            rollingMaxActiveThreads = 0,

                        });
                    }
                }
                
            }

            return hystrixThreadPoolList;
        }

        private string RefinePercentileString(string data)
        {
            if (data == null)
                return data;

            Dictionary<string, string> keyToRefined = new Dictionary<string,string>()
            {
                { "\"P0\":", "\"0\":" },
                { "\"P25\":", "\"25\":" },
                { "\"P50\":", "\"50\":" },
                { "\"P75\":", "\"75\":" },
                { "\"P90\":", "\"90\":" },
                { "\"P95\":", "\"95\":" },
                { "\"P99\":", "\"99\":" },
                { "\"P99DOT5\":", "\"99.5\":" },
                { "\"P100\":", "\"100\":" },
            };

            foreach (KeyValuePair<string, string> item in keyToRefined)
            {
                data = data.Replace(item.Key, item.Value);
            }

            return data;
        }
    }

    [DataContract]
    internal class HystrixCommandInfo
    {
        [DataMember]
        public string type { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string group { get; set; }

        [DataMember]
        public long currentTime { get; set; }

        [DataMember]
        public bool isCircuitBreakerOpen { get; set; }

        [DataMember]
        public long errorPercentage { get; set; }

        [DataMember]
        public long errorCount { get; set; }

        [DataMember]
        public long requestCount { get; set; }

        [DataMember]
        public long rollingCountCollapsedRequests { get; set; }

        [DataMember]
        public long rollingCountExceptionsThrown { get; set; }

        [DataMember]
        public long rollingCountFailure { get; set; }

        [DataMember]
        public long rollingCountFallbackFailure { get; set; }

        [DataMember]
        public long rollingCountFallbackRejection { get; set; }

        [DataMember]
        public long rollingCountFallbackSuccess { get; set; }

        [DataMember]
        public long rollingCountResponsesFromCache { get; set; }

        [DataMember]
        public long rollingCountSemaphoreRejected { get; set; }

        [DataMember]
        public long rollingCountShortCircuited { get; set; }

        [DataMember]
        public long rollingCountSuccess { get; set; }

        [DataMember]
        public long rollingCountThreadPoolRejected { get; set; }

        [DataMember]
        public long rollingCountTimeout { get; set; }

        [DataMember]
        public long currentConcurrentExecutionCount { get; set; }

        [DataMember]
        public long latencyExecute_mean { get; set; }

        [DataMember]
        public PercentileInfo latencyExecute { get; set; }

        [DataMember]
        public long latencyTotal_mean { get; set; }

        [DataMember]
        public PercentileInfo latencyTotal { get; set; }

        [DataMember]
        public int propertyValue_circuitBreakerRequestVolumeThreshold { get; set; }

        [DataMember]
        public long propertyValue_circuitBreakerSleepWindowInMilliseconds { get; set; }

        [DataMember]
        public int propertyValue_circuitBreakerErrorThresholdPercentage { get; set; }

        [DataMember]
        public bool propertyValue_circuitBreakerForceOpen { get; set; }

        [DataMember]
        public bool propertyValue_circuitBreakerForceClosed { get; set; }

        [DataMember]
        public bool propertyValue_circuitBreakerEnabled { get; set; }

        [DataMember]
        public string propertyValue_executionIsolationStrategy { get; set; }

        [DataMember]
        public long propertyValue_executionIsolationThreadTimeoutInMilliseconds { get; set; }

        [DataMember]
        public long propertyValue_executionIsolationThreadInterruptOnTimeout { get; set; }

        [DataMember]
        public long propertyValue_executionIsolationThreadPoolKeyOverride { get; set; }

        [DataMember]
        public long propertyValue_executionIsolationSemaphoreMaxConcurrentRequests { get; set; }

        [DataMember]
        public long propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests { get; set; }

        [DataMember]
        public long propertyValue_metricsRollingStatisticalWindowInMilliseconds { get; set; }

        [DataMember]
        public bool propertyValue_requestCacheEnabled { get; set; }

        [DataMember]
        public bool propertyValue_requestLogEnabled { get; set; }

        [DataMember]
        public int reportingHosts { get; set; }
    }

    [DataContract]
    internal class PercentileInfo
    {
        [DataMember]
        public long P0 { get; set; }

        [DataMember]
        public long P25 { get; set; }

        [DataMember]
        public long P50 { get; set; }

        [DataMember]
        public long P75 { get; set; }

        [DataMember]
        public long P90 { get; set; }

        [DataMember]
        public long P95 { get; set; }

        [DataMember]
        public long P99 { get; set; }

        [DataMember]
        public long P99DOT5 { get; set; }

        [DataMember]
        public long P100 { get; set; }
    }

    [DataContract]
    internal class HystrixThreadPoolInfo
    {
        [DataMember]
        public string type { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public long currentTime { get; set; }

        [DataMember]
        public int currentActiveCount { get; set; }

        [DataMember]
        public int currentCompletedTaskCount { get; set; }

        [DataMember]
        public int currentCorePoolSize { get; set; }

        [DataMember]
        public int currentLargestPoolSize { get; set; }

        [DataMember]
        public int currentMaximumPoolSize { get; set; }

        [DataMember]
        public int currentPoolSize { get; set; }

        [DataMember]
        public int currentQueueSize { get; set; }

        [DataMember]
        public int currentTaskCount { get; set; }

        [DataMember]
        public int rollingCountThreadsExecuted { get; set; }

        [DataMember]
        public int rollingMaxActiveThreads { get; set; }

        [DataMember]
        public int propertyValue_queueSizeRejectionThreshold { get; set; }

        [DataMember]
        public int propertyValue_metricsRollingStatisticalWindowInMilliseconds { get; set; }

        [DataMember]
        public int reportingHosts { get; set; }
    }
}
