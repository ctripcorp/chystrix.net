using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Timers;
using System.Runtime.Serialization;
using System.Configuration;
using System.Threading;

using CHystrix.Utils;
using CHystrix.Utils.Extensions;
using CHystrix.Config;

namespace CHystrix.Registration
{
    internal class SelfRegistrationManager
    {
        public const int RegistrationIntervalMilliseconds = 20 * 60 * 1000;
        const string RegistryServiceOperationName = "RegisterApp";

        private static System.Timers.Timer _timer;

        private static object _lock = new object();

        public static void Start()
        {
            _timer = new System.Timers.Timer()
            {
                Interval = RegistrationIntervalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += new ElapsedEventHandler(RegisterData);
        }

        public static void Stop()
        {
            try
            {
                if (_timer != null)
                {
                    using (_timer)
                    {
                        _timer.Stop();
                    }
                }
            }
            catch
            {
            }
        }

        private static void RegisterData(object sender, ElapsedEventArgs arg)
        {
            try
            {
                if (!Monitor.TryEnter(_lock))
                    return;

                try
                {
                    if (string.IsNullOrWhiteSpace(HystrixCommandBase.RegistryServiceUrl))
                    {
                        CommonUtils.Log.Log(LogLevelEnum.Warning, "Hystrix Registry Url is empty.",
                            new Dictionary<string, string>().AddLogTagData("FXD303043"));
                        return;
                    }

                    string registerAppUrl = HystrixCommandBase.RegistryServiceUrl.WithTrailingSlash() + RegistryServiceOperationName + ".json";
                    RegisterAppRequest request = new RegisterAppRequest()
                    {
                        ApplicationPath = HystrixCommandBase.ApplicationPath,
                        AppName = HystrixCommandBase.HystrixAppName,
                        HostIP = CommonUtils.HostIP,
                        HystrixVersion = HystrixCommandBase.HystrixVersion,
                        HystrixCommands = getRegisterCommandInfos()
                    };
                    registerAppUrl.PostJsonToUrl(request.ToJson());
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to register data to Hystrix Registry Service: " + HystrixCommandBase.RegistryServiceUrl, ex,
                    new Dictionary<string, string>().AddLogTagData("FXD303044"));
            }
        }

        private static List<RegisterCommandInfo> getRegisterCommandInfos()
        {
            Dictionary<String, RegisterCommandInfo> registerCommandInfoMap = new Dictionary<string, RegisterCommandInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (CommandComponents commandComponents in HystrixCommandBase.CommandComponentsCollection.Values)
            {
                CommandInfo commandInfo = commandComponents.CommandInfo;
                RegisterCommandInfo registerCommandInfo;
                registerCommandInfoMap.TryGetValue(commandInfo.CommandKey, out registerCommandInfo);
                if (registerCommandInfo == null)
                {
                    registerCommandInfo = new RegisterCommandInfo()
                    {
                        Key = commandInfo.CommandKey,
                        Domain = commandInfo.Domain,
                        GroupKey = commandInfo.GroupKey,
                        InstanceKeys = new List<string>(),
                        Type = commandInfo.Type
                    };
                    registerCommandInfoMap[commandInfo.CommandKey] = registerCommandInfo;
                }

                if (commandInfo.InstanceKey != null)
                    registerCommandInfo.InstanceKeys.Add(commandInfo.InstanceKey);
            }

            return registerCommandInfoMap.Values.ToList();
        }
    }

    [DataContract]
    internal partial class RegisterAppRequest
    {
        [DataMember()]
        public string AppName { get; set; }

        [DataMember()]
        public string HostIP { get; set; }

        [DataMember()]
        public string ApplicationPath { get; set; }

        [DataMember()]
        public string HystrixVersion { get; set; }

        [DataMember()]
        public List<RegisterCommandInfo> HystrixCommands { get; set; }
    }

    [DataContract]
    internal class RegisterCommandInfo
    {
        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public string GroupKey { get; set; }

        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public List<string> InstanceKeys { get; set; }

        [DataMember]
        public string Type { get; set; }

    }

}
