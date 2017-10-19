using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using CHystrix.Utils;
using CHystrix.Utils.Extensions;
using CHystrix.Utils.Web;

namespace CHystrix.Config
{
    internal static class CommandConfigSyncManager
    {
        public const int SyncConfigIntervalMilliseconds = 30 * 1000;
        const string ConfigServiceOperationName = "GetApplicationConfig";

        private static Timer Timer;

        public static void Start()
        {
            if (Timer != null)
                return;

            SyncConfig(null, null);
            Timer = new Timer()
            {
                Interval = SyncConfigIntervalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            Timer.Elapsed += new ElapsedEventHandler(SyncConfig);
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

        private static void SyncConfig(object sender, ElapsedEventArgs arg)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(HystrixCommandBase.ConfigServiceUrl))
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "Config Service Url is empty.",
                        new Dictionary<string,string>().AddLogTagData("FXD303011"));
                    return;
                }

                string configServiceOperationUrl = HystrixCommandBase.ConfigServiceUrl.WithTrailingSlash() + ConfigServiceOperationName + ".json";
                GetApplicationConfigRequestType request = new GetApplicationConfigRequestType() { AppName = HystrixCommandBase.HystrixAppName.ToLower() };
                string responseJson = configServiceOperationUrl.PostJsonToUrl(request.ToJson());
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "Got null response from config service: " + HystrixCommandBase.ConfigServiceUrl,
                        new Dictionary<string,string>().AddLogTagData("FXD303012"));
                    return;
                }

                GetApplicationConfigResponseType response = responseJson.FromJson<GetApplicationConfigResponseType>();
                if (response == null)
                    return;

                if (response.DefaultConfig != null)
                {
                    if (response.DefaultConfig.CircuitBreakerErrorThresholdPercentage.HasValue
                        && response.DefaultConfig.CircuitBreakerErrorThresholdPercentage.Value >= ComponentFactory.MinGlobalDefaultCircuitBreakerErrorThresholdPercentage
                        && response.DefaultConfig.CircuitBreakerErrorThresholdPercentage.Value <= 100)
                        ComponentFactory.GlobalDefaultCircuitBreakerErrorThresholdPercentage = response.DefaultConfig.CircuitBreakerErrorThresholdPercentage;

                    if (response.DefaultConfig.CircuitBreakerForceClosed.HasValue)
                        ComponentFactory.GlobalDefaultCircuitBreakerForceClosed = response.DefaultConfig.CircuitBreakerForceClosed;

                    if (response.DefaultConfig.CircuitBreakerRequestCountThreshold.HasValue
                        && response.DefaultConfig.CircuitBreakerRequestCountThreshold.Value >= ComponentFactory.MinGlobalDefaultCircuitBreakerRequestCountThreshold)
                        ComponentFactory.GlobalDefaultCircuitBreakerRequestCountThreshold = response.DefaultConfig.CircuitBreakerRequestCountThreshold;

                    if (response.DefaultConfig.CommandMaxConcurrentCount.HasValue
                        && response.DefaultConfig.CommandMaxConcurrentCount.Value >= ComponentFactory.MinGlobalDefaultCommandMaxConcurrentCount)
                        ComponentFactory.GlobalDefaultCommandMaxConcurrentCount = response.DefaultConfig.CommandMaxConcurrentCount;

                    if (response.DefaultConfig.CommandTimeoutInMilliseconds.HasValue
                        && response.DefaultConfig.CommandTimeoutInMilliseconds.Value >= ComponentFactory.MinGlobalDefaultCommandTimeoutInMilliseconds)
                        ComponentFactory.GlobalDefaultCommandTimeoutInMilliseconds = response.DefaultConfig.CommandTimeoutInMilliseconds;

                    if (response.DefaultConfig.FallbackMaxConcurrentCount.HasValue
                        && response.DefaultConfig.FallbackMaxConcurrentCount.Value >= ComponentFactory.MinGlobalDefaultFallbackMaxConcurrentCount)
                        ComponentFactory.GlobalDefaultFallbackMaxConcurrentCount = response.DefaultConfig.FallbackMaxConcurrentCount;
                }

                bool noValuableConfig = response.Application == null || response.Application.Commands == null || response.Application.Commands.Count == 0;
                Dictionary<string, CommandComponents> commandComponentsCollection = HystrixCommandBase.CommandComponentsCollection.ToDictionary(p => p.Key, p => p.Value);
                foreach (KeyValuePair<string, CommandComponents> pair in commandComponentsCollection)
                {
                    ICommandConfigSet configSet = pair.Value.ConfigSet;

                    if (noValuableConfig)
                    {
                        configSet.RaiseConfigChangeEvent();
                        continue;
                    }

                    CHystrixCommand command = response.Application.Commands.Where(c => string.Compare(c.Key, pair.Value.CommandInfo.CommandKey, true) == 0).FirstOrDefault();
                    if (command == null || command.Config == null)
                    {
                        configSet.RaiseConfigChangeEvent();
                        continue;
                    }

                    if (command.Config.CircuitBreakerErrorThresholdPercentage.HasValue
                        && command.Config.CircuitBreakerErrorThresholdPercentage.Value > 0 && command.Config.CircuitBreakerErrorThresholdPercentage.Value <= 100)
                        configSet.CircuitBreakerErrorThresholdPercentage = command.Config.CircuitBreakerErrorThresholdPercentage.Value;
                    if (command.Config.CircuitBreakerForceClosed.HasValue)
                        configSet.CircuitBreakerForceClosed = command.Config.CircuitBreakerForceClosed.Value;
                    if (command.Config.CircuitBreakerForceOpen.HasValue)
                        configSet.CircuitBreakerForceOpen = command.Config.CircuitBreakerForceOpen.Value;
                    if (command.Config.CircuitBreakerRequestCountThreshold.HasValue && command.Config.CircuitBreakerRequestCountThreshold.Value > 0)
                        configSet.CircuitBreakerRequestCountThreshold = command.Config.CircuitBreakerRequestCountThreshold.Value;
                    if (command.Config.CommandMaxConcurrentCount.HasValue && command.Config.CommandMaxConcurrentCount.Value > 0)
                        configSet.CommandMaxConcurrentCount = command.Config.CommandMaxConcurrentCount.Value;
                    if (command.Config.CommandTimeoutInMilliseconds.HasValue && command.Config.CommandTimeoutInMilliseconds.Value > 0)
                        configSet.CommandTimeoutInMilliseconds = command.Config.CommandTimeoutInMilliseconds.Value;
                    if (command.Config.DegradeLogLevel.HasValue)
                        configSet.DegradeLogLevel = command.Config.DegradeLogLevel.Value;
                    if (command.Config.LogExecutionError.HasValue)
                        configSet.LogExecutionError = command.Config.LogExecutionError.Value;
                    if (command.Config.FallbackMaxConcurrentCount.HasValue && command.Config.FallbackMaxConcurrentCount.Value > 0)
                        configSet.FallbackMaxConcurrentCount = command.Config.FallbackMaxConcurrentCount.Value;
                    if (command.Config.MaxAsyncCommandExceedPercentage.HasValue && command.Config.MaxAsyncCommandExceedPercentage.Value >= 0
                        && command.Config.MaxAsyncCommandExceedPercentage.Value <= 100)
                        configSet.MaxAsyncCommandExceedPercentage = command.Config.MaxAsyncCommandExceedPercentage.Value;

                    configSet.RaiseConfigChangeEvent();
                }
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to sync config from config service: " + HystrixCommandBase.ConfigServiceUrl, ex,
                    new Dictionary<string,string>().AddLogTagData("FXD303014"));
            }
        }
    }
}
