using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Timers;
using System.Runtime.Serialization;

using CHystrix.Utils;
using CHystrix.Utils.Extensions;
using CHystrix.Utils.Web;

namespace CHystrix.Config
{
    internal static class HystrixConfigSyncManager
    {
        const string ConfigWebServiceSettingKey = "FxConfigServiceUrl";
        const string ConfigWebServiceUrlSuffix = "ServiceConfig/ConfigInfoes/Get/921807";

        const string ConfigWebServiceCHystrixConfigServiceUrlName = "CHystrix_ConfigService_Url";
        const string ConfigWebServiceSOARegistryServiceUrlName = "SOA_RegistryService_Url";

        const string CHystrixConfigServiceName = "CHystrixConfigService";
        const string CHystrixConfigServiceNamespace = "http://soa.ctrip.com/framework/chystrix/configservice/v1";

        const string CHystrixRegistryServiceName = "CHystrixRegistryService";
        const string CHystrixRegistryServiceNamespace = "http://soa.ctrip.com/framework/soa/chystrix/registryservice/v1";

        const string SOARegistryServiceOperationName = "LookupServiceUrl";

        public const int SyncConfigIntervalMilliseconds = 10 * 60 * 1000;

        public static string ConfigWebServiceUrl { get; private set; }
        public static string SOARegistryServiceUrl { get; private set; }

        private static string _chystrixConfigWebServiceUrl;

        private static Timer _timer;

        public static void Start()
        {
            ConfigWebServiceUrl = ConfigurationManager.AppSettings[ConfigWebServiceSettingKey];
            if (string.IsNullOrWhiteSpace(ConfigWebServiceUrl))
            {
                ConfigWebServiceUrl = null;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, "No FxConfigWebService setting is found in appSettings.",
                    new Dictionary<string, string>().AddLogTagData("FXD303029"));
                return;
            }
            else
                ConfigWebServiceUrl = ConfigWebServiceUrl.Trim();

            _chystrixConfigWebServiceUrl = ConfigWebServiceUrl.WithTrailingSlash() + ConfigWebServiceUrlSuffix;

            if (_timer != null)
                return;

            SyncFXConfigWebServiceSettings();
            string hystrixRegistryServiceUrl = SyncSOAServiceUrl(CHystrixRegistryServiceName, CHystrixRegistryServiceNamespace);
            if (!string.IsNullOrWhiteSpace(hystrixRegistryServiceUrl))
                HystrixCommandBase.RegistryServiceUrl = hystrixRegistryServiceUrl;

            _timer = new Timer()
            {
                Interval = SyncConfigIntervalMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += new ElapsedEventHandler(SyncConfig);
        }

        public static void Reset()
        {
            try
            {
                if (_timer == null)
                    return;

                Timer timer = _timer;
                _timer = null;
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
                if (string.IsNullOrWhiteSpace(SOARegistryServiceUrl))
                    SyncFXConfigWebServiceSettings();

                string url = SyncSOAServiceUrl(CHystrixConfigServiceName, CHystrixConfigServiceNamespace);
                if (!string.IsNullOrWhiteSpace(url))
                    HystrixCommandBase.ConfigServiceUrl = url;

                url = SyncSOAServiceUrl(CHystrixRegistryServiceName, CHystrixRegistryServiceNamespace);
                if (!string.IsNullOrWhiteSpace(url))
                    HystrixCommandBase.RegistryServiceUrl = url;
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Hystrix Config Sync failed", ex,
                    new Dictionary<string, string>().AddLogTagData("FXD303045"));
            }
        }

        private static void SyncFXConfigWebServiceSettings()
        {
            try
            {
                string responseJson = _chystrixConfigWebServiceUrl.GetJsonFromUrl();
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "Got null response from config web service: " + _chystrixConfigWebServiceUrl,
                        new Dictionary<string,string>().AddLogTagData("FXD303030"));
                    return;
                }
                List<ConfigWebServiceConfigItem> response = responseJson.FromJson<List<ConfigWebServiceConfigItem>>();
                if (response == null || response.Count == 0)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "Response has no config data: " + _chystrixConfigWebServiceUrl,
                        new Dictionary<string,string>().AddLogTagData("FXD303031"));
                    return;
                }

                foreach (ConfigWebServiceConfigItem item in response)
                {
                    if (item != null && string.Compare(item.Name, ConfigWebServiceCHystrixConfigServiceUrlName, true) == 0
                        && !string.IsNullOrWhiteSpace(item.Value))
                    {
                        HystrixCommandBase.ConfigServiceUrl = item.Value.Trim();
                    }

                    if (item != null && string.Compare(item.Name, ConfigWebServiceSOARegistryServiceUrlName, true) == 0
                        && !string.IsNullOrWhiteSpace(item.Value))
                    {
                        SOARegistryServiceUrl = item.Value.Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(SOARegistryServiceUrl) || string.IsNullOrWhiteSpace(HystrixCommandBase.ConfigServiceUrl))
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "No config url is got from config web service: " + _chystrixConfigWebServiceUrl,
                        new Dictionary<string,string>().AddLogTagData("FXD303032"));
                }
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to sync config from config web service: " + _chystrixConfigWebServiceUrl, ex,
                    new Dictionary<string,string>().AddLogTagData("FXD303033"));
            }
        }

        private static string SyncSOAServiceUrl(string serviceName, string serviceNamespace)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SOARegistryServiceUrl))
                    return null;

                string lookupServiceUrlOperationUrl = SOARegistryServiceUrl.WithTrailingSlash() + SOARegistryServiceOperationName + ".json";
                LookupServiceUrlRequest request = new LookupServiceUrlRequest()
                {
                    ServiceName = serviceName,
                    ServiceNamespace = serviceNamespace
                };
                string jsonResponse = lookupServiceUrlOperationUrl.PostJsonToUrl(request.ToJson());
                if (string.IsNullOrWhiteSpace(jsonResponse))
                    return null;

                LookupServiceUrlResponse response = jsonResponse.FromJson<LookupServiceUrlResponse>();
                if (response == null || string.IsNullOrWhiteSpace(response.targetUrl))
                    return null;

                return response.targetUrl;
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to sync SOA service url from SOA registry service: " + SOARegistryServiceUrl, ex,
                    new Dictionary<string, string>().AddLogTagData("FXD303046"));
                return null;
            }
        }
    }

    [DataContract]
    internal class ConfigWebServiceConfigItem
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Value { get; set; }
    }

    [DataContract]
    internal partial class LookupServiceUrlRequest
    {
        [DataMember()]
        public string ServiceName { get; set; }

        [DataMember()]
        public string ServiceNamespace { get; set; }
    }
    
    [DataContract]
    internal partial class LookupServiceUrlResponse
    {
        [DataMember()]
        public string targetUrl { get; set; }
    }
}
