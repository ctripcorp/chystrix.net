using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace CHystrix.Utils.TransparentIntegration
{
    // CHystrix has the method since version 1.2.0.0
    internal static partial class CHystrixIntegration
    {
        private static Action<string, string, string, string> _configCommand3;
        private static Action<string, string, string, string, int> _configCommand4;
 
        private static Func<string, string, Func<object>, Func<object>, object> _runCommand2;

        private static Action<string, string, string, string> _utilsSemaphoreIsolationConfig3;
        private static Action<string, string, string, string, int> _utilsSemaphoreIsolationConfig4;
        private static Func<string, string, object> _utilsSemaphoreIsolationCreateInstance2;

        public static void ConfigCommand(string instanceKey, string commandKey, string groupKey, string domain)
        {
            if (_configCommand3 != null)
            {
                _configCommand3(instanceKey, commandKey, groupKey, domain);
                return;
            }

            ConfigCommand(commandKey, groupKey, domain);
        }

        public static void ConfigCommand(string instancekey, string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            if (_configCommand4 != null)
            {
                _configCommand4(instancekey, commandKey, groupKey, domain, maxConcurrentCount);
                return;
            }

            ConfigCommand(commandKey, groupKey, domain, maxConcurrentCount);
        }

        public static T RunCommand<T>(string instanceKey, string commandKey, Func<object> execute, Func<object> getFallback)
        {
            if (_runCommand2 != null)
                return (T)_runCommand2(instanceKey, commandKey, execute, getFallback);

            return RunCommand<T>(commandKey, execute);
        }

        public static void UtilsSemaphoreIsolationConfig(string instanceKey, string commandKey, string groupKey, string domain)
        {
            if (_utilsSemaphoreIsolationConfig3 != null)
                _utilsSemaphoreIsolationConfig3(instanceKey, commandKey, groupKey, domain);

            UtilsSemaphoreIsolationConfig(commandKey, groupKey, domain);
        }

        public static void UtilsSemaphoreIsolationConfig(string instanceKey, string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            if (_utilsSemaphoreIsolationConfig4 != null)
                _utilsSemaphoreIsolationConfig4(instanceKey, commandKey, groupKey, domain, maxConcurrentCount);

            UtilsSemaphoreIsolationConfig(commandKey, groupKey, domain, maxConcurrentCount);
        }

        public static object UtilsSemaphoreIsolationCreateInstance(string instanceKey, string commandKey)
        {
            if (_utilsSemaphoreIsolationCreateInstance2 != null)
                return _utilsSemaphoreIsolationCreateInstance2(instanceKey, commandKey);

            return UtilsSemaphoreIsolationCreateInstance(commandKey);
        }

        private static void InitInstanceKeyMethods(Type hystrixCommandType, Type utilsSemaphoreIsolationType)
        {
            try
            {
                _configCommand3 = MakeConfigCommand3Delegate(hystrixCommandType);
                _configCommand4 = MakeConfigCommand4Delegate(hystrixCommandType);
                _runCommand2 = MakeRunCommand2Delegate(hystrixCommandType);
                _utilsSemaphoreIsolationConfig3 = UtilsSemaphoreIsolationMakeConfig3Delegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationConfig4 = UtilsSemaphoreIsolationMakeConfig4Delegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationCreateInstance2 = UtilsSemaphoreIsolationMakeCreateInstance2Delegate(utilsSemaphoreIsolationType);
                if (_configCommand3 == null || _configCommand4 == null || _runCommand2 == null || _utilsSemaphoreIsolationConfig3 == null
                    || _utilsSemaphoreIsolationConfig4 == null || _utilsSemaphoreIsolationCreateInstance2 == null)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Info, "The CHystrix.dll version is low. No required instance key methods ware made.", new Dictionary<string, string>() { { "ErrorCode", "FXD301020" } });
                    return;
                }

                CommonUtils.Log.Log(LogLevelEnum.Info, "Successfully inited CHystrix instance key methods.");
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Info, "The CHystrix.dll version is low. IP based circuit breaker will be not used. Use domain based circuit breaker.", ex,
                    new Dictionary<string, string>() { { "ErrorCode", "FXD301021" } });
            }
        }

        private static Action<string, string, string, string> MakeConfigCommand3Delegate(Type hystrixCommandType)
        {
            Type stringType = typeof(string);
            var instanceKeyParameter = Expression.Parameter(stringType, "instanceKey");
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var groupKeyParameter = Expression.Parameter(stringType, "groupKey");
            var domainParameter = Expression.Parameter(stringType, "domain");
            Expression callMethod = Expression.Call(
                hystrixCommandType,
                CHystrixConfigCommandGenericMethodName,
                new Type[] { typeof(object) },
                new Expression[]
                {
                    Expression.Convert(instanceKeyParameter, stringType),
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType)
                });
            return Expression.Lambda<Action<string, string, string, string>>(callMethod, instanceKeyParameter, commandKeyParameter, groupKeyParameter, domainParameter).Compile();
        }

        private static Action<string, string, string, string, int> MakeConfigCommand4Delegate(Type hystrixCommandType)
        {
            Type stringType = typeof(string);
            Type intType = typeof(int);
            var instanceKeyParameter = Expression.Parameter(stringType, "instanceKey");
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
                    Expression.Convert(instanceKeyParameter, stringType),
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType),
                    Expression.Convert(maxConcurrentCountParameter, intType)
                });
            return Expression.Lambda<Action<string, string, string, string, int>>(
                callMethod, instanceKeyParameter, commandKeyParameter, groupKeyParameter, domainParameter, maxConcurrentCountParameter).Compile();
        }

        private static Func<string, string, Func<object>, Func<object>, object> MakeRunCommand2Delegate(Type hystrixCommandType)
        {
            Type stringType = typeof(string);
            Type funcType = typeof(Func<object>);
            var instanceKeyParameter = Expression.Parameter(stringType, "instanceKey");
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var executeParameter = Expression.Parameter(funcType, "execute");
            var getFallbackParameter = Expression.Parameter(funcType, "getFallback");
            Expression callMethod = Expression.Call(
                hystrixCommandType,
                CHystrixRunCommandGenericMethodName,
                new Type[] { typeof(object) },
                new Expression[]
                {
                    Expression.Convert(instanceKeyParameter, stringType),
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(executeParameter, funcType),
                    Expression.Convert(getFallbackParameter, funcType),
                });
            return Expression.Lambda<Func<string, string, Func<object>, Func<object>, object>>(callMethod, instanceKeyParameter, commandKeyParameter, executeParameter, getFallbackParameter).Compile();
        }

        private static Action<string, string, string, string> UtilsSemaphoreIsolationMakeConfig3Delegate(Type utilsSemaphoreIsolationType)
        {
            Type stringType = typeof(string);
            var instanceKeyParameter = Expression.Parameter(stringType, "instanceKey");
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var groupKeyParameter = Expression.Parameter(stringType, "groupKey");
            var domainParameter = Expression.Parameter(stringType, "domain");
            Expression callMethod = Expression.Call(
                utilsSemaphoreIsolationType,
                CHystrixUtilsSemaphoreIsolationConfigMethodName,
                new Type[] { },
                new Expression[]
                {
                    Expression.Convert(instanceKeyParameter, stringType),
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType)
                });
            return Expression.Lambda<Action<string, string, string, string>>(callMethod, instanceKeyParameter, commandKeyParameter, groupKeyParameter, domainParameter).Compile();
        }

        private static Action<string, string, string, string, int> UtilsSemaphoreIsolationMakeConfig4Delegate(Type utilsSemaphoreIsolationType)
        {
            Type stringType = typeof(string);
            Type intType = typeof(int);
            var instanceKeyParameter = Expression.Parameter(stringType, "instanceKey");
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var groupKeyParameter = Expression.Parameter(stringType, "groupKey");
            var domainParameter = Expression.Parameter(stringType, "domain");
            var maxConcurrentCountParameter = Expression.Parameter(intType, "maxConcurrentCount");
            Expression callMethod = Expression.Call(
                utilsSemaphoreIsolationType,
                CHystrixUtilsSemaphoreIsolationConfigMethodName,
                new Type[] { },
                new Expression[]
                {
                    Expression.Convert(instanceKeyParameter, stringType),
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType),
                    Expression.Convert(maxConcurrentCountParameter, intType)
                });
            return Expression.Lambda<Action<string, string, string, string, int>>(
                callMethod, instanceKeyParameter, commandKeyParameter, groupKeyParameter, domainParameter, maxConcurrentCountParameter).Compile();
        }

        private static Func<string, string, object> UtilsSemaphoreIsolationMakeCreateInstance2Delegate(Type utilsSemaphoreIsolationType)
        {
            Type stringType = typeof(string);
            var instanceKeyParameter = Expression.Parameter(stringType, "instanceKey");
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            Expression callMethod = Expression.Call(
                utilsSemaphoreIsolationType,
                CHystrixUtilsSemaphoreIsolationCreateInstanceMethodName,
                new Type[] { },
                new Expression[]
                {
                    Expression.Convert(instanceKeyParameter, stringType),
                    Expression.Convert(commandKeyParameter, stringType)
                });
            return Expression.Lambda<Func<string, string, object>>(callMethod, instanceKeyParameter, commandKeyParameter).Compile();
        }

    }
}
