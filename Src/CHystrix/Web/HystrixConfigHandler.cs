using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Runtime.Serialization;

using CHystrix.Utils.Web;
using CHystrix.Utils.Extensions;
using CHystrix.Config;
using CHystrix.Registration;

namespace CHystrix.Web
{
    internal class HystrixConfigHandler : IHttpHandler
    {
        public const string OperationName = "_config";

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                Dictionary<string, CommandComponents> commandComponentsCollection =
                    HystrixCommandBase.CommandComponentsCollection.ToDictionary(p => p.Key, p => p.Value);
                CHystrixConfigInfo config = new CHystrixConfigInfo()
                {
                    ApplicationPath = HystrixCommandBase.ApplicationPath,
                    CHystrixAppName = HystrixCommandBase.HystrixAppName,
                    CHystrixVersion = HystrixCommandBase.HystrixVersion,
                    MaxCommandCount = HystrixCommandBase.MaxCommandCount,
                    CommandCount = commandComponentsCollection.Count,
                    ConfigWebServiceUrl = HystrixConfigSyncManager.ConfigWebServiceUrl,
                    SOARegistryServiceUrl = HystrixConfigSyncManager.SOARegistryServiceUrl,
                    CHystrixConfigServiceUrl = HystrixCommandBase.ConfigServiceUrl,
                    CHystrixRegistryServiceUrl = HystrixCommandBase.RegistryServiceUrl,
                    SyncCHystrixConfigIntervalMilliseconds = HystrixConfigSyncManager.SyncConfigIntervalMilliseconds,
                    SyncCommandConfigIntervalMilliseconds = CommandConfigSyncManager.SyncConfigIntervalMilliseconds,
                    SelfRegistrationIntervalMilliseconds = SelfRegistrationManager.RegistrationIntervalMilliseconds,
                    FrameworkDefaultCircuitBreakerRequestCountThreshold = ComponentFactory.FrameworkDefaultCircuitBreakerRequestCountThreshold,
                    FrameworkDefaultCircuitBreakerErrorThresholdPercentage = ComponentFactory.FrameworkDefaultCircuitBreakerErrorThresholdPercentage,
                    FrameworkDefaultCircuitBreakerForceClosed = ComponentFactory.FrameworkDefaultCircuitBreakerForceClosed,
                    FrameworkDefaultCommandTimeoutInMilliseconds = ComponentFactory.FrameworkDefaultCommandTimeoutInMilliseconds,
                    FrameworkDefaultSemaphoreIsolationMaxConcurrentCount = ComponentFactory.FrameworkDefaultSemaphoreIsolationMaxConcurrentCount,
                    FrameworkDefaultThreadIsolationMaxConcurrentCount = ComponentFactory.FrameworkDefaultThreadIsolationMaxConcurrentCount,
                    MinGlobalDefaultCircuitBreakerRequestCountThreshold = ComponentFactory.MinGlobalDefaultCircuitBreakerRequestCountThreshold,
                    MinGlobalDefaultCircuitBreakerErrorThresholdPercentage = ComponentFactory.MinGlobalDefaultCircuitBreakerErrorThresholdPercentage,
                    MinGlobalDefaultCommandTimeoutInMilliseconds = ComponentFactory.MinGlobalDefaultCommandTimeoutInMilliseconds,
                    MinGlobalDefaultCommandMaxConcurrentCount = ComponentFactory.MinGlobalDefaultCommandMaxConcurrentCount,
                    MinGlobalDefaultFallbackMaxConcurrentCount = ComponentFactory.MinGlobalDefaultFallbackMaxConcurrentCount,
                    GlobalDefaultCircuitBreakerRequestCountThreshold = ComponentFactory.GlobalDefaultCircuitBreakerRequestCountThreshold,
                    GlobalDefaultCircuitBreakerErrorThresholdPercentage = ComponentFactory.GlobalDefaultCircuitBreakerErrorThresholdPercentage,
                    GlobalDefaultCircuitBreakerForceClosed = ComponentFactory.GlobalDefaultCircuitBreakerForceClosed,
                    GlobalDefaultCommandTimeoutInMilliseconds = ComponentFactory.GlobalDefaultCommandTimeoutInMilliseconds,
                    GlobalDefaultCommandMaxConcurrentCount = ComponentFactory.GlobalDefaultCommandMaxConcurrentCount,
                    GlobalDefaultFallbackMaxConcurrentCount = ComponentFactory.GlobalDefaultFallbackMaxConcurrentCount,
                    DefaultCircuitBreakerRequestCountThreshold = ComponentFactory.DefaultCircuitBreakerRequestCountThreshold,
                    DefaultCircuitBreakerErrorThresholdPercentage = ComponentFactory.DefaultCircuitBreakerErrorThresholdPercentage,
                    DefaultCircuitBreakerForceClosed = ComponentFactory.DefaultCircuitBreakerForceClosed,
                    DefaultCommandTimeoutInMilliseconds = ComponentFactory.DefaultCommandTimeoutInMilliseconds,
                    DefaultSemaphoreIsolationMaxConcurrentCount = ComponentFactory.DefaultSemaphoreIsolationMaxConcurrentCount,
                    DefaultThreadIsolationMaxConcurrentCount = ComponentFactory.DefaultThreadIsolationMaxConcurrentCount,
                    CommandConfigInfoList = new List<CommandConfigInfo>()
                };
                foreach (CommandComponents item in commandComponentsCollection.Values)
                {
                    config.CommandConfigInfoList.Add(new CommandConfigInfo()
                        {
                            CommandKey = item.CommandInfo.CommandKey,
                            GroupKey = item.CommandInfo.GroupKey,
                            Domain = item.CommandInfo.Domain,
                            Type = item.CommandInfo.Type,
                            ConfigSet = item.ConfigSet as CommandConfigSet
                        });
                }
                config.CommandConfigInfoList = config.CommandConfigInfoList.Distinct().ToList();
                context.Response.ContentType = HttpContentTypes.Json;
                context.Response.Write(config.ToJson());
            }
            catch (Exception ex)
            {
                context.Response.ContentType = HttpContentTypes.PlainText;
                context.Response.Write(ex.Message);
            }
        }
    }

    [DataContract]
    internal class CHystrixConfigInfo
    {
        [DataMember(Order=1)]
        public string ApplicationPath { get; set; }

        [DataMember(Order=2)]
        public string CHystrixAppName { get; set; }

        [DataMember(Order=3)]
        public string CHystrixVersion { get; set; }

        [DataMember(Order=4)]
        public int MaxCommandCount { get; set; }

        [DataMember(Order=5)]
        public int CommandCount { get; set; }

        [DataMember(Order=6)]
        public string ConfigWebServiceUrl { get; set; }

        [DataMember(Order=7)]
        public string SOARegistryServiceUrl { get; set; }

        [DataMember(Order=8)]
        public string CHystrixConfigServiceUrl { get; set; }

        [DataMember(Order=9)]
        public string CHystrixRegistryServiceUrl { get; set; }

        [DataMember(Order=10)]
        public int SyncCHystrixConfigIntervalMilliseconds { get; set; }

        [DataMember(Order=11)]
        public int SyncCommandConfigIntervalMilliseconds {get;set;}

        [DataMember(Order=12)]
        public int SelfRegistrationIntervalMilliseconds {get;set;}

        [DataMember(Order=13)]
        public int FrameworkDefaultCircuitBreakerRequestCountThreshold { get; set; }

        [DataMember(Order=14)]
        public int FrameworkDefaultCircuitBreakerErrorThresholdPercentage { get; set; }

        [DataMember(Order=15)]
        public bool FrameworkDefaultCircuitBreakerForceClosed { get; set; }

        [DataMember(Order=16)]
        public int FrameworkDefaultCommandTimeoutInMilliseconds { get; set; }

        [DataMember(Order=17)]
        public int FrameworkDefaultSemaphoreIsolationMaxConcurrentCount { get; set; }

        [DataMember(Order=18)]
        public int FrameworkDefaultThreadIsolationMaxConcurrentCount { get; set; }

        [DataMember(Order=19)]
        public int MinGlobalDefaultCircuitBreakerRequestCountThreshold { get; set; }

        [DataMember(Order=20)]
        public int MinGlobalDefaultCircuitBreakerErrorThresholdPercentage { get; set; }

        [DataMember(Order=21)]
        public int MinGlobalDefaultCommandMaxConcurrentCount { get; set; }

        [DataMember(Order=22)]
        public int MinGlobalDefaultFallbackMaxConcurrentCount { get; set; }

        [DataMember(Order=23)]
        public int MinGlobalDefaultCommandTimeoutInMilliseconds { get; set; }

        [DataMember(Order=24)]
        public int? GlobalDefaultCircuitBreakerRequestCountThreshold { get; set; }

        [DataMember(Order=25)]
        public int? GlobalDefaultCircuitBreakerErrorThresholdPercentage { get; set; }

        [DataMember(Order=26)]
        public bool? GlobalDefaultCircuitBreakerForceClosed { get; set; }

        [DataMember(Order=27)]
        public int? GlobalDefaultCommandTimeoutInMilliseconds { get; set; }

        [DataMember(Order=28)]
        public int? GlobalDefaultCommandMaxConcurrentCount { get; set; }

        [DataMember(Order=29)]
        public int? GlobalDefaultFallbackMaxConcurrentCount { get; set; }

        [DataMember(Order=30)]
        public int? DefaultCircuitBreakerRequestCountThreshold { get; set; }

        [DataMember(Order=31)]
        public int? DefaultCircuitBreakerErrorThresholdPercentage { get; set; }

        [DataMember(Order=32)]
        public bool? DefaultCircuitBreakerForceClosed { get; set; }

        [DataMember(Order=33)]
        public int? DefaultCommandTimeoutInMilliseconds { get; set; }

        [DataMember(Order=34)]
        public int? DefaultSemaphoreIsolationMaxConcurrentCount { get; set; }

        [DataMember(Order=35)]
        public int? DefaultThreadIsolationMaxConcurrentCount { get; set; }

        [DataMember(Order=36)]
        public List<CommandConfigInfo> CommandConfigInfoList { get; set; }
    }

    [DataContract]
    internal class CommandConfigInfo
    {
        [DataMember(Order=1)]
        public string CommandKey { get; set; }

        [DataMember(Order=2)]
        public string GroupKey { get; set; }

        [DataMember(Order=3)]
        public string Domain { get; set; }

        [DataMember(Order=4)]
        public string Type { get; set; }

        [DataMember(Order=5)]
        public CommandConfigSet ConfigSet { get; set; }

        public override bool Equals(object obj)
        {
            CommandConfigInfo other = obj as CommandConfigInfo;
            if (other == null)
                return false;

            return string.Equals(CommandKey, other.CommandKey, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return CommandKey == null ? 0 : CommandKey.GetHashCode();
        }
 
    }
}
