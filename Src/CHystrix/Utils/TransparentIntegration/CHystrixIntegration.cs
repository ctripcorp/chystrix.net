using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace CHystrix.Utils.TransparentIntegration
{
    internal static partial class CHystrixIntegration
    {
        const string CHystrixAssemblyName = "CHystrix";
        const string CHystrixCommandTypeName = "CHystrix.HystrixCommandBase";

        const string CHystrixRunCommandGenericMethodName = "RunCommand";
        const string CHystrixConfigCommandGenericMethodName = "ConfigCommand";

        public const string CHystrixExceptionName = "CHystrix.HystrixException";

        public static bool HasCHystrix { get; private set; }

        private static Action<string, string, string> _configCommand;
        private static Action<string, string, string, int> _configCommand2;

        private static Func<string, Func<object>, object> _runCommand;

        static CHystrixIntegration()
        {
            try
            {
                Assembly chystrixAssembly = GetHystrixAssembly();
                if (chystrixAssembly == null)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "No CHystrix assembly exists.", new Dictionary<string, string>() { { "ErrorCode", "FXD301007" } });
                    return;
                }

                Type hystrixCommandType = chystrixAssembly.GetType(CHystrixCommandTypeName);
                if (hystrixCommandType == null)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Error, "No HystrixCommand type exists in the CHystrix assembly.", new Dictionary<string, string>() { { "ErrorCode", "FXD301008" } });
                    return;
                }

                _configCommand = MakeConfigCommandDelegate(hystrixCommandType);
                _configCommand2 = MakeConfigCommand2Delegate(hystrixCommandType);
                _runCommand = MakeRunCommandDelegate(hystrixCommandType);
                if (_configCommand == null || _configCommand2 == null || _runCommand == null)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "No commands (ConfigCommand & RunCommand) were made.", new Dictionary<string, string>() { { "ErrorCode", "FXD301009" } });
                    return;
                }

                HasCHystrix = true;
                CommonUtils.Log.Log(LogLevelEnum.Info, "Successfully inited the CHystrix basic help methods.", new Dictionary<string, string>() { { "ErrorCode", "FXD301031" } });

                InitRegisterCustomBadRequestExceptionMethod(hystrixCommandType);

                Type utilsSemaphoreIsolationType = chystrixAssembly.GetType(CHystrixUtilsSemaphoreIsolationTypeName);
                if (utilsSemaphoreIsolationType == null)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Info, "The CHystrix.dll version is low. No Semaphore Isolation help methods in it.", 
                        new Dictionary<string, string>() { { "ErrorCode", "FXD301011" } });
                    return;
                }

                InitUtilsSemaphoreIsolationHelpMethods(utilsSemaphoreIsolationType);

                InitInstanceKeyMethods(hystrixCommandType, utilsSemaphoreIsolationType);
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to init the CHystrix help methods.", ex, new Dictionary<string, string>() { { "ErrorCode", "FXD301010" } });
            }
        }

        public static void ConfigCommand(string commandKey, string groupKey, string domain)
        {
            _configCommand(commandKey, groupKey, domain);
        }

        public static void ConfigCommand(string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            _configCommand2(commandKey, groupKey, domain, maxConcurrentCount);
        }

        public static T RunCommand<T>(string commandKey, Func<object> execute)
        {
            return (T)_runCommand(commandKey, execute);
        }

        public static void RegisterCustomBadRequestExceptionChecker(string name, Func<Exception, bool> customBadRequestExceptionChecker)
        {
            // CHystrix has the method since version 1.1.0.0
            if (_registerCustomBadRequestExceptionChecker == null)
                return;

            _registerCustomBadRequestExceptionChecker(name, customBadRequestExceptionChecker);
        }

        private static Assembly GetHystrixAssembly()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (string.Compare(assembly.GetName().Name, CHystrixAssemblyName, true) == 0)
                {
                    return assembly;
                }
            }

            try
            {
                return Assembly.Load(CHystrixAssemblyName);
            }
            catch
            {
            }

            return null;
        }

        private static Action<string, string, string> MakeConfigCommandDelegate(Type hystrixCommandType)
        {
            Type stringType = typeof(string);
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var groupKeyParameter = Expression.Parameter(stringType, "groupKey");
            var domainParameter = Expression.Parameter(stringType, "domain");
            Expression callMethod = Expression.Call(
                hystrixCommandType,
                CHystrixConfigCommandGenericMethodName,
                new Type[] { typeof(object) },
                new Expression[]
                {
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType)
                });
            return Expression.Lambda<Action<string, string, string>>(callMethod, commandKeyParameter, groupKeyParameter, domainParameter).Compile();
        }

        private static Action<string, string, string, int> MakeConfigCommand2Delegate(Type hystrixCommandType)
        {
            Type stringType = typeof(string);
            Type intType = typeof(int);
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var groupKeyParameter = Expression.Parameter(stringType, "groupKey");
            var domainParameter = Expression.Parameter(stringType, "domain");
            var maxConcurrentCountParameter = Expression.Parameter(intType, "maxConcurrentCount");
            Expression callMethod = Expression.Call(
                hystrixCommandType,
                CHystrixConfigCommandGenericMethodName,
                new Type[] { typeof(object) },
                new Expression[]
                {
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType),
                    Expression.Convert(maxConcurrentCountParameter, intType)
                });
            return Expression.Lambda<Action<string, string, string, int>>(
                callMethod, commandKeyParameter, groupKeyParameter, domainParameter, maxConcurrentCountParameter).Compile();
        }

        private static Func<string, Func<object>, object> MakeRunCommandDelegate(Type hystrixCommandType)
        {
            Type stringType = typeof(string);
            Type funcType = typeof(Func<object>);
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var executeParameter = Expression.Parameter(funcType, "execute");
            Expression callMethod = Expression.Call(
                hystrixCommandType,
                CHystrixRunCommandGenericMethodName,
                new Type[] { typeof(object) },
                new Expression[]
                {
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(executeParameter, funcType),
                });
            return Expression.Lambda<Func<string, Func<object>, object>>(callMethod, commandKeyParameter, executeParameter).Compile();
        }

    }
}
