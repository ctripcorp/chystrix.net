using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Configuration;
using System.Timers;
using System.Threading.Tasks;

using CHystrix.Config;
using CHystrix.Metrics;
using CHystrix.CircuitBreaker;
using CHystrix.Log;
using CHystrix.Utils;
using CHystrix.Registration;
using CHystrix.Utils.Atomic;

namespace CHystrix
{
    public abstract class HystrixCommandBase : ICommand
    {
        #region Const

        internal const string HystrixAppNameSettingKey = "CHystrix.AppName";

        internal const string HystrixConfigServiceUrlSettingKey = "CHystrix.ConfigServiceUrl";
        internal const string HystrixRegistryServiceUrlSettingKey = "CHystrix.RegistryServiceUrl";

        internal const string HystrixMaxCommandCountSettingKey = "CHystrix.MaxCommandCount";

        internal const string DefaultAppName = "HystrixApp";

        internal const string DefaultGroupKey = "DefaultGroup";
        internal const int DefaultMaxCommandCount = 10000;

        #endregion

        #region Static Members

        internal static string HystrixAppName { get; private set; }
        internal static string HystrixVersion { get; private set; }

        internal static readonly ConcurrentDictionary<string, CommandComponents> CommandComponentsCollection =
            new ConcurrentDictionary<string, CommandComponents>(StringComparer.InvariantCultureIgnoreCase);
        internal static readonly AtomicInteger CommandCount = new AtomicInteger();

        internal static readonly ConcurrentDictionary<string, IsolationSemaphore> ExecutionSemaphores =
            new ConcurrentDictionary<string, IsolationSemaphore>(StringComparer.InvariantCultureIgnoreCase);
        internal static readonly ConcurrentDictionary<string, IsolationSemaphore> FallbackExecutionSemaphores =
            new ConcurrentDictionary<string, IsolationSemaphore>(StringComparer.InvariantCultureIgnoreCase);

        internal static readonly ConcurrentDictionary<Type, string> TypePredefinedKeyMappings = new ConcurrentDictionary<Type, string>();

        internal static string ConfigServiceUrl { get; set; }
        internal static string RegistryServiceUrl { get; set; }

        internal static int MaxCommandCount { get; private set; }

        internal static string ApplicationPath { get; set; }

        static HystrixCommandBase()
        {
            HystrixAppName = ConfigurationManager.AppSettings[HystrixAppNameSettingKey];
            if (string.IsNullOrWhiteSpace(HystrixAppName))
            {
                if (string.IsNullOrWhiteSpace(CommonUtils.AppId))
                {
                    string message = "Either CHystrix.AppName Or AppId must be configured.";
                    CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string,string>().AddLogTagData("FXD303000"));
                    HystrixAppName = DefaultAppName;
                }
                else
                    HystrixAppName = CommonUtils.AppId;
            }
            else
            {
                HystrixAppName = HystrixAppName.Trim();
                if (!HystrixAppName.IsValidHystrixName())
                {
                    string message = "HystrixAppName setting is invalid: " + HystrixAppName + ". Name pattern is: " + CommonUtils.HystrixNamePattern;
                    CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303001"));
                    throw new ArgumentException(message);
                }

                if (CommonUtils.AppId != null)
                    HystrixAppName = CommonUtils.AppId + "-" + HystrixAppName;
            }

            HystrixVersion = typeof(HystrixCommandBase).Assembly.GetName().Version.ToString();

            int maxCommandCount;
            if (!int.TryParse(ConfigurationManager.AppSettings[HystrixMaxCommandCountSettingKey], out maxCommandCount) || maxCommandCount <= 0)
                maxCommandCount = DefaultMaxCommandCount;
            MaxCommandCount = maxCommandCount;

            ConfigServiceUrl = ConfigurationManager.AppSettings[HystrixConfigServiceUrlSettingKey];
            if (string.IsNullOrWhiteSpace(ConfigServiceUrl))
                ConfigServiceUrl = null;
            else
                ConfigServiceUrl = ConfigServiceUrl.Trim();

            RegistryServiceUrl = ConfigurationManager.AppSettings[HystrixRegistryServiceUrlSettingKey];
            if (string.IsNullOrWhiteSpace(RegistryServiceUrl))
                RegistryServiceUrl = null;
            else
                RegistryServiceUrl = RegistryServiceUrl.Trim();

            if (!string.IsNullOrWhiteSpace(HystrixAppName))
            {
                HystrixConfigSyncManager.Start();
                CommandConfigSyncManager.Start();
                MetricsReporter.Start();
                SelfRegistrationManager.Start();
            }
        }

        internal static void Reset()
        {
            CommandComponentsCollection.Clear();

            ExecutionSemaphores.Clear();
            FallbackExecutionSemaphores.Clear();

            CommandCount.GetAndSet(0);

            CustomBadRequestExceptionChecker.BadRequestExceptionCheckers.Clear();
        }

        private static void OnConfigChanged(string key, ICommandConfigSet configSet)
        {
            if (string.IsNullOrWhiteSpace(key) || configSet == null)
                return;

            IsolationSemaphore executionSemaphore;
            if (configSet.CommandMaxConcurrentCount > 0 && ExecutionSemaphores.TryGetValue(key, out executionSemaphore)
                && executionSemaphore != null && executionSemaphore.Count != configSet.CommandMaxConcurrentCount)
                    executionSemaphore.Count = configSet.CommandMaxConcurrentCount;

            IsolationSemaphore fallbackExecutionSemaphore;
            if (configSet.FallbackMaxConcurrentCount > 0 && FallbackExecutionSemaphores.TryGetValue(key, out fallbackExecutionSemaphore)
                && fallbackExecutionSemaphore!= null && fallbackExecutionSemaphore.Count != configSet.FallbackMaxConcurrentCount)
                    fallbackExecutionSemaphore.Count = configSet.FallbackMaxConcurrentCount;
        }

        internal static CommandComponents CreateCommandComponents(string key, string instanceKey, string commandKey, string groupKey, string domain,
            IsolationModeEnum isolationMode, Action<ICommandConfigSet> config, Type type)
        {
            if (!key.IsValidHystrixName())
            {
                string message = "Hystrix command key has invalid char: " + key + ". Name pattern is: " + CommonUtils.HystrixNamePattern;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string,string>().AddLogTagData("FXD303004"));
                throw new ArgumentException(message);
            }
 
            if (!string.IsNullOrWhiteSpace(instanceKey) && !instanceKey.IsValidHystrixName())
            {
                string message = "Hystrix command instanceKey has invalid char: " + instanceKey + ". Name pattern is: " + CommonUtils.HystrixNamePattern;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string,string>().AddLogTagData("FXD303004"));
                throw new ArgumentException(message);
            }

            if (!commandKey.IsValidHystrixName())
            {
                string message = "Hystrix command commandKey has invalid char: " + commandKey + ". Name pattern is: " + CommonUtils.HystrixNamePattern;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string,string>().AddLogTagData("FXD303004"));
                throw new ArgumentException(message);
            }

            if (!groupKey.IsValidHystrixName())
            {
                string message = "Hystrix command group commandKey has invalid char: " + groupKey + ". Name pattern is: " + CommonUtils.HystrixNamePattern;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303005"));
                throw new ArgumentException(message);
            }

            if (!domain.IsValidHystrixName())
            {
                string message = "Hystrix domain has invalid char: " + domain + ". Name pattern is: " + CommonUtils.HystrixNamePattern;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303006"));
                throw new ArgumentException(message);
            }

            if (CommandCount >= MaxCommandCount)
            {
                string message = "Hystrix command count has reached the limit: " + MaxCommandCount;
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303007"));
                throw new ArgumentException(message);
            }
            CommandCount.IncrementAndGet();

            ICommandConfigSet configSet = ComponentFactory.CreateCommandConfigSet(isolationMode);
            configSet.SubcribeConfigChangeEvent(c => { OnConfigChanged(key, c); });
            try
            {
                if (config != null)
                    config(configSet);
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Fatal, "Failed to config command: " + key, ex,
                    new Dictionary<string,string>().AddLogTagData("FXD303008"));
            }

            ICommandMetrics metrics = ComponentFactory.CreateCommandMetrics(configSet, key, isolationMode);

            return new CommandComponents()
            {
                ConfigSet = configSet,
                Metrics = metrics,
                CircuitBreaker = ComponentFactory.CreateCircuitBreaker(configSet, metrics),
                CommandInfo = new CommandInfo
                {
                    Domain = domain.ToLower(),
                    GroupKey = groupKey.ToLower(),
                    CommandKey = commandKey.ToLower(),
                    InstanceKey = instanceKey == null ? null : instanceKey.ToLower(),
                    Key = key.ToLower(),
                    Type = isolationMode.ToString()
                },
                Log = ComponentFactory.CreateLog(configSet, type),
                IsolationMode = isolationMode
            };
        }

        /// <summary>
        /// For custom bad request exception use
        /// Circuit Breaker will ignore the bad request, bad request doesn't cause circuit breaker open.
        /// </summary>
        /// <param name="name">Meaningful key to identify the custom bad request exception checker.</param>
        /// <param name="isBadRequestExceptionDelegate">The custom bad request exception checker.</param>
        public static void RegisterCustomBadRequestExceptionChecker(string name, Func<Exception, bool> isBadRequestExceptionDelegate)
        {
            if (string.IsNullOrWhiteSpace(name) || isBadRequestExceptionDelegate == null)
                throw new ArgumentNullException("name or delegate is null.");

            CustomBadRequestExceptionChecker.BadRequestExceptionCheckers.GetOrAdd(name, isBadRequestExceptionDelegate);
        }

        #region Config Command

        public static void ConfigCommand<T>(string commandKey, Action<ICommandConfigSet> configCommand)
        {
            ConfigCommand<T>(commandKey, null, configCommand);
        }

        public static void ConfigCommand<T>(string commandKey, string domain, Action<ICommandConfigSet> configCommand)
        {
            ConfigCommand<T>(commandKey, null, domain, configCommand);
        }

        public static void ConfigCommand<T>(string commandKey, string domain)
        {
            ConfigCommand<T>(commandKey, null, domain);
        }

        public static void ConfigCommand<T>(string commandKey, string groupKey, string domain)
        {
            ConfigCommand<T>(null, commandKey, groupKey, domain);
        }

        public static void ConfigCommand<T>(string instanceKey, string commandKey, string groupKey, string domain)
        {
            ConfigCommand<T>(instanceKey, commandKey, groupKey, domain, null);
        }

        public static void ConfigCommand<T>(string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            ConfigCommand<T>(null, commandKey, groupKey, domain, maxConcurrentCount);
        }

        public static void ConfigCommand<T>(string instanceKey, string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            ConfigCommand<T>(instanceKey, commandKey, groupKey, domain, maxConcurrentCount, fallbackMaxConcurrentCount: maxConcurrentCount);
        }

        public static void ConfigCommand<T>(string commandKey, string groupKey, string domain, int maxConcurrentCount, int timeoutInMilliseconds)
        {
            ConfigCommand<T>(null, commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds);
        }

        public static void ConfigCommand<T>(string instanceKey, string commandKey, string groupKey, string domain, int maxConcurrentCount, int timeoutInMilliseconds)
        {
            ConfigCommand<T>(instanceKey, commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds, fallbackMaxConcurrentCount: maxConcurrentCount);
        }

        internal static void ConfigCommand<T>(string commandKey, string groupKey, string domain, int? maxConcurrentCount = null,
            int? timeoutInMilliseconds = null, int? circuitBreakerRequestCountThreshold = null, int? circuitBreakerErrorThresholdPercentage = null,
            int? fallbackMaxConcurrentCount = null)
        {
            ConfigCommand<T>(null, commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds, circuitBreakerRequestCountThreshold, circuitBreakerErrorThresholdPercentage, fallbackMaxConcurrentCount);
        }

        internal static void ConfigCommand<T>(string instanceKey, string commandKey, string groupKey, string domain, int? maxConcurrentCount = null,
            int? timeoutInMilliseconds = null, int? circuitBreakerRequestCountThreshold = null, int? circuitBreakerErrorThresholdPercentage = null,
            int? fallbackMaxConcurrentCount = null)
        {
            ConfigCommand<T>(instanceKey, commandKey, groupKey, domain,
                configSet =>
                {
                    if (maxConcurrentCount.HasValue)
                        configSet.CommandMaxConcurrentCount = maxConcurrentCount.Value;
                    if (timeoutInMilliseconds.HasValue)
                        configSet.CommandTimeoutInMilliseconds = timeoutInMilliseconds.Value;
                    if (circuitBreakerRequestCountThreshold.HasValue)
                        configSet.CircuitBreakerRequestCountThreshold = circuitBreakerRequestCountThreshold.Value;
                    if (circuitBreakerErrorThresholdPercentage.HasValue)
                        configSet.CircuitBreakerErrorThresholdPercentage = circuitBreakerErrorThresholdPercentage.Value;
                    if (fallbackMaxConcurrentCount.HasValue)
                        configSet.FallbackMaxConcurrentCount = fallbackMaxConcurrentCount.Value;
                });
        }

        public static void ConfigCommand<T>(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> configCommand)
        {
            ConfigCommand<T>(null, commandKey, groupKey, domain, configCommand);
        }

        public static void ConfigCommand<T>(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> configCommand)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                string message = "HystrixCommand Key cannot be null.";
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303002"));
                throw new ArgumentNullException(message);
            }

            new GenericSemaphoreIsolationCommand<T>(instanceKey, commandKey, groupKey, domain, () => { return default(T); }, null, configCommand);
        }

        public static void ConfigAsyncCommand<T>(string commandKey, Action<ICommandConfigSet> configCommand)
        {
            ConfigAsyncCommand<T>(commandKey, null, configCommand);
        }

        public static void ConfigAsyncCommand<T>(string commandKey, string domain, Action<ICommandConfigSet> configCommand)
        {
            ConfigAsyncCommand<T>(commandKey, null, domain, configCommand);
        }

        public static void ConfigAsyncCommand<T>(string commandKey, string domain)
        {
            ConfigAsyncCommand<T>(commandKey, null, domain);
        }

        public static void ConfigAsyncCommand<T>(string commandKey, string groupKey, string domain)
        {
            ConfigAsyncCommand<T>(commandKey, groupKey, domain, null);
        }

        public static void ConfigAsyncCommand<T>(string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            ConfigAsyncCommand<T>(commandKey, groupKey, domain, maxConcurrentCount, fallbackMaxConcurrentCount: maxConcurrentCount);
        }

        public static void ConfigAsyncCommand<T>(string commandKey, string groupKey, string domain, int maxConcurrentCount, int timeoutInMilliseconds)
        {
            ConfigAsyncCommand<T>(commandKey, groupKey, domain, maxConcurrentCount, timeoutInMilliseconds, fallbackMaxConcurrentCount: maxConcurrentCount);
        }

        internal static void ConfigAsyncCommand<T>(string commandKey, string groupKey, string domain, int? maxConcurrentCount = null,
            int? timeoutInMilliseconds = null, int? circuitBreakerRequestCountThreshold = null, int? circuitBreakerErrorThresholdPercentage = null,
            int? fallbackMaxConcurrentCount = null, int? maxAsyncCommandExceedPercentage = null)
        {
            ConfigAsyncCommand<T>(commandKey, groupKey, domain,
                configSet =>
                {
                    if (maxConcurrentCount.HasValue)
                        configSet.CommandMaxConcurrentCount = maxConcurrentCount.Value;
                    if (timeoutInMilliseconds.HasValue)
                        configSet.CommandTimeoutInMilliseconds = timeoutInMilliseconds.Value;
                    if (circuitBreakerRequestCountThreshold.HasValue)
                        configSet.CircuitBreakerRequestCountThreshold = circuitBreakerRequestCountThreshold.Value;
                    if (circuitBreakerErrorThresholdPercentage.HasValue)
                        configSet.CircuitBreakerErrorThresholdPercentage = circuitBreakerErrorThresholdPercentage.Value;
                    if (fallbackMaxConcurrentCount.HasValue)
                        configSet.FallbackMaxConcurrentCount = fallbackMaxConcurrentCount.Value;
                    if (maxAsyncCommandExceedPercentage.HasValue)
                        configSet.MaxAsyncCommandExceedPercentage = maxAsyncCommandExceedPercentage.Value;
                });
        }

        public static void ConfigAsyncCommand<T>(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> configCommand)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                string message = "HystrixCommand Key cannot be null.";
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303003"));
                throw new ArgumentNullException(message);
            }

            new GenericThreadIsolationCommand<T>(commandKey, groupKey, domain, () => { return default(T); }, null, configCommand);
        }

        #endregion

        #region Run Command

        public static T RunCommand<T>(string commandKey, Func<T> execute)
        {
            return RunCommand<T>(null, commandKey, execute);
        }

        public static T RunCommand<T>(string instanceKey, string commandKey, Func<T> execute)
        {
            return RunCommand<T>(instanceKey, commandKey, execute, null);
        }

        public static T RunCommand<T>(string commandKey, Func<T> execute, Func<T> getFallback)
        {
            return RunCommand<T>(null, commandKey, execute, getFallback);
        }

        public static T RunCommand<T>(string instanceKey, string commandKey, Func<T> execute, Func<T> getFallback)
        {
            return RunCommand<T>(instanceKey, commandKey, null, null, execute, getFallback, null);
        }

        internal static T RunCommand<T>(
            string commandKey, string groupKey, string domain, Func<T> execute, Func<T> getFallback, Action<ICommandConfigSet> configCommand)
        {
            return RunCommand<T>(null, commandKey, groupKey, domain, execute, getFallback, configCommand);
        }

        internal static T RunCommand<T>(string instanceKey,
            string commandKey, string groupKey, string domain, Func<T> execute, Func<T> getFallback, Action<ICommandConfigSet> configCommand)
        {
            SemaphoreIsolationCommand<T> command = new GenericSemaphoreIsolationCommand<T>(instanceKey, commandKey, groupKey, domain, execute, getFallback, configCommand);
            return command.Run();
        }

        #endregion

        #region Run threadisolation command

        public static Task<T> RunCommandAsync<T>(string commandKey, Func<T> execute)
        {
            return RunCommandAsync<T>(commandKey, execute, null);
        }

        public static Task<T> RunCommandAsync<T>(string commandKey, Func<T> execute, Func<T> getFallback)
        {
            return RunCommandAsync<T>(commandKey, null, null, execute, getFallback, null);
        }

        internal static Task<T> RunCommandAsync<T>(
            string commandKey, string groupKey, string domain, Func<T> execute, Func<T> getFallback, Action<ICommandConfigSet> configCommand)
        {
            var command = new GenericThreadIsolationCommand<T>(commandKey, groupKey, domain, execute, getFallback, configCommand);
            return command.RunAsync();
        }

        #endregion

        #endregion

        #region Instance Members

        public virtual CommandStatusEnum Status { get; internal set; }

        /// <summary>
        /// A concrete command has to only have 1 single Domain for its class level use
        /// It should be a constant and never be changed no matter what app it is
        /// </summary>
        private string _domain;
        public virtual string Domain
        {
            get { return _domain; }
        }

        private string _groupKey;
        /// <summary>
        /// A concrete command has to only have 1 single group key for its class level use
        /// It should be a constant and never be changed no matter what app it is
        /// </summary>
        public virtual string GroupKey
        {
            get { return _groupKey; }
        }

        private string _key;
        /// <summary>
        /// A concrete command has to only have 1 key for its class level use
        /// It should be a constant and never be changed no matter what app it is
        /// </summary>
        public virtual string Key
        {
            get { return _key; }
        }

        private string _instanceKey;

        public virtual string InstanceKey
        {
            get { return _instanceKey; }
        }

        private string _commandKey;

        public virtual string CommandKey
        {
            get { return _commandKey; }
        }

        internal ICommandConfigSet ConfigSet { get; private set; }

        internal CommandConfigSet ConfigSetForTest
        {
            get
            {
                return ConfigSet as CommandConfigSet;
            }
        }

        internal ICommandMetrics Metrics { get; private set; }

        internal ICircuitBreaker CircuitBreaker { get; private set; }

        internal ILog Log { get; private set; }

        internal readonly object ExecutionLock = new object();

        internal abstract IsolationModeEnum IsolationMode { get; }

        internal HystrixCommandBase()
            : this(null, null, null, null)
        {
        }


        internal HystrixCommandBase(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
            : this(null, commandKey, groupKey, domain, config)
        {
        }

        internal HystrixCommandBase(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
        {
            _instanceKey = string.IsNullOrWhiteSpace(instanceKey) ? null : instanceKey.Trim();
            _commandKey = string.IsNullOrWhiteSpace(commandKey) ? null : commandKey.Trim();
            _groupKey = string.IsNullOrWhiteSpace(groupKey) ? DefaultGroupKey : groupKey.Trim();
            _domain = string.IsNullOrWhiteSpace(domain) ? CommandDomains.Default : domain.Trim();
            _config = config;

            Type type = this.GetType();

            if (string.IsNullOrWhiteSpace(Key))
            {
                if (string.IsNullOrWhiteSpace(CommandKey))
                {
                    if (string.IsNullOrWhiteSpace(_commandKey))
                    {
                        if (type.IsGenericType)
                            throw new ArgumentNullException("CommandKey cannot be null.");

                        _commandKey = TypePredefinedKeyMappings.GetOrAdd(type, t => CommonUtils.GenerateTypeKey(t));
                    }
                }
            }

            _key = CommonUtils.GenerateKey(InstanceKey, CommandKey);

            CommandComponents commandComponents = CommandComponentsCollection.GetOrAdd(Key, key =>
                CreateCommandComponents(key, InstanceKey, CommandKey, GroupKey, Domain, IsolationMode, Config, type));

            if (commandComponents.IsolationMode != IsolationMode)
            {
                string message = "The key " + Key + " has been used for " + commandComponents.IsolationMode + ". Now it cannot be used for " + IsolationMode + ".";
                CommonUtils.Log.Log(LogLevelEnum.Fatal, message, new Dictionary<string, string>().AddLogTagData("FXD303009"));
                throw new ArgumentException(message);
            }

            CircuitBreaker = commandComponents.CircuitBreaker;
            Log = commandComponents.Log;
            ConfigSet = commandComponents.ConfigSet;
            Metrics = commandComponents.Metrics;

            _groupKey = commandComponents.CommandInfo.GroupKey;
            _commandKey = commandComponents.CommandInfo.CommandKey;
            _instanceKey = commandComponents.CommandInfo.InstanceKey;
            _domain = commandComponents.CommandInfo.Domain;
        }

        /// <summary>
        /// The method will be called when the first command instance is created.
        /// </summary>
        private Action<ICommandConfigSet> _config;
        protected virtual void Config(ICommandConfigSet configSet)
        {
            if (_config != null)
                _config(configSet);
        }

        internal Dictionary<string, string> GetLogTagInfo()
        {
            return new Dictionary<string, string>()
            {
                { "Domain", Domain },
                { "Type", IsolationMode.ToString() },
                { "GroupKey", GroupKey },
                { "CommandKey", CommandKey },
                { "InstanceKey", InstanceKey },
                { "Key", Key }
            };
        }

        #endregion
    }

    public abstract class HystrixCommandBase<T> : HystrixCommandBase
    {
        private bool _hasFallback;
        internal virtual bool HasFallback
        {
            get { return _hasFallback; }
        }

        internal HystrixCommandBase()
            : this(null, null, null, null)
        {
        }

        internal HystrixCommandBase(string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
            : this(null, commandKey, groupKey, domain, config)
        { 
        }

        internal HystrixCommandBase(string instanceKey, string commandKey, string groupKey, string domain, Action<ICommandConfigSet> config)
            : base(instanceKey, commandKey, groupKey, domain, config)
        {
            _hasFallback = this is IFallback<T>;
        }

        protected abstract T Execute();

        internal IFallback<T> ToIFallback()
        {
            return this as IFallback<T>;
        }
    }

    internal enum IsolationModeEnum
    {
        SemaphoreIsolation, ThreadIsolation
    }

    [DataContract]
    internal class CommandInfo
    {
        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public string GroupKey { get; set; }

        [DataMember]
        public string CommandKey { get; set; }

        [DataMember]
        public string InstanceKey { get; set; }

        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public string Type { get; set; }
    }

    internal class CommandComponents
    {
        public ICommandConfigSet ConfigSet { get; set; }
        public ICommandMetrics Metrics { get; set; }
        public CommandInfo CommandInfo { get; set; }

        public ICircuitBreaker CircuitBreaker { get; set; }
        public ILog Log { get; set; }

        public IsolationModeEnum IsolationMode { get; set; }
    }
}
