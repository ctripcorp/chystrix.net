using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace CHystrix.Utils.TransparentIntegration
{
    internal static partial class CHystrixIntegration
    {
        const string CHystrixUtilsSemaphoreIsolationTypeName = "CHystrix.Utils.SemaphoreIsolation";
        const string CHystrixUtilsSemaphoreIsolationConfigMethodName = "Config";
        const string CHystrixUtilsSemaphoreIsolationCreateInstanceMethodName = "CreateInstance";
        const string CHystrixUtilsSemaphoreIsolationStartExecutionMethodName = "StartExecution";
        const string CHystrixUtilsSemaphoreIsolationMarkSuccessMethodName = "MarkSuccess";
        const string CHystrixUtilsSemaphoreIsolationMarkFailureMethodName = "MarkFailure";
        const string CHystrixUtilsSemaphoreIsolationMarkBadRequestMethodName = "MarkBadRequest";
        const string CHystrixUtilsSemaphoreIsolationEndExecutionMethodName = "EndExecution";

        public static bool HasCHystrixIsolationUtils { get; private set; }

        private static Action<string, string, string> _utilsSemaphoreIsolationConfig;
        private static Action<string, string, string, int> _utilsSemaphoreIsolationConfig2;
        private static Func<string, object> _utilsSemaphoreIsolationCreateInstance;
        private static Action<object> _utilsSemaphoreIsolationStartExecution;
        private static Action<object> _utilsSemaphoreIsolationMarkSuccess;
        private static Action<object> _utilsSemaphoreIsolationMarkFailure;
        private static Action<object> _utilsSemaphoreIsolationMarkBadRequest;
        private static Action<object> _utilsSemaphoreIsolationEndExecution;

        private static void InitUtilsSemaphoreIsolationHelpMethods(Type utilsSemaphoreIsolationType)
        {
            try
            {
                _utilsSemaphoreIsolationConfig = UtilsSemaphoreIsolationMakeConfigDelegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationConfig2 = UtilsSemaphoreIsolationMakeConfig2Delegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationCreateInstance = UtilsSemaphoreIsolationMakeCreateInstanceDelegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationStartExecution = UtilsSemaphoreIsolationMakeStartExecutionDelegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationMarkSuccess = UtilsSemaphoreIsolationMakeMarkSuccessDelegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationMarkFailure = UtilsSemaphoreIsolationMakeMarkFailureDelegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationMarkBadRequest = UtilsSemaphoreIsolationMakeMarkBadRequestDelegate(utilsSemaphoreIsolationType);
                _utilsSemaphoreIsolationEndExecution = UtilsSemaphoreIsolationMakeEndExecutionDelegate(utilsSemaphoreIsolationType);

                if (_utilsSemaphoreIsolationConfig == null || _utilsSemaphoreIsolationConfig2 == null
                    || _utilsSemaphoreIsolationCreateInstance == null || _utilsSemaphoreIsolationStartExecution == null
                    || _utilsSemaphoreIsolationMarkSuccess == null || _utilsSemaphoreIsolationMarkFailure == null
                    || _utilsSemaphoreIsolationMarkBadRequest == null || _utilsSemaphoreIsolationEndExecution == null)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Info, "The CHystrix.dll version is low. No required Semaphore Isolation help commands were made.", new Dictionary<string, string>() { { "ErrorCode", "FXD301012" } });
                    return;
                }

                HasCHystrixIsolationUtils = true;
                CommonUtils.Log.Log(LogLevelEnum.Info, "Successfully inited the CHystrix Semaphore Isolation help methods.");
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Info, "The CHystrix.dll version is low. Failed to init the CHystrix Semaphore Isolation help methods.", ex, new Dictionary<string, string>() { { "ErrorCode", "FXD301013" } });
            }
        }

        public static void UtilsSemaphoreIsolationConfig(string commandKey, string groupKey, string domain)
        {
            _utilsSemaphoreIsolationConfig(commandKey, groupKey, domain);
        }

        public static void UtilsSemaphoreIsolationConfig(string commandKey, string groupKey, string domain, int maxConcurrentCount)
        {
            _utilsSemaphoreIsolationConfig2(commandKey, groupKey, domain, maxConcurrentCount);
        }

        public static object UtilsSemaphoreIsolationCreateInstance(string commandKey)
        {
            return _utilsSemaphoreIsolationCreateInstance(commandKey);
        }

        public static void UtilsSemaphoreIsolationStartExecution(object instance)
        {
            _utilsSemaphoreIsolationStartExecution(instance);
        }

        public static void UtilsSemaphoreIsolationMarkSuccess(object instance)
        {
            _utilsSemaphoreIsolationMarkSuccess(instance);
        }

        public static void UtilsSemaphoreIsolationMarkFailure(object instance)
        {
            _utilsSemaphoreIsolationMarkFailure(instance);
        }

        public static void UtilsSemaphoreIsolationMarkBadRequest(object instance)
        {
            _utilsSemaphoreIsolationMarkBadRequest(instance);
        }

        public static void UtilsSemaphoreIsolationEndExecution(object instance)
        {
            _utilsSemaphoreIsolationEndExecution(instance);
        }

        private static Action<string, string, string> UtilsSemaphoreIsolationMakeConfigDelegate(Type utilsSemaphoreIsolationType)
        {
            Type stringType = typeof(string);
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            var groupKeyParameter = Expression.Parameter(stringType, "groupKey");
            var domainParameter = Expression.Parameter(stringType, "domain");
            Expression callMethod = Expression.Call(
                utilsSemaphoreIsolationType,
                CHystrixUtilsSemaphoreIsolationConfigMethodName,
                new Type[] { },
                new Expression[]
                {
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType)
                });
            return Expression.Lambda<Action<string, string, string>>(callMethod, commandKeyParameter, groupKeyParameter, domainParameter).Compile();
        }

        private static Action<string, string, string, int> UtilsSemaphoreIsolationMakeConfig2Delegate(Type utilsSemaphoreIsolationType)
        {
            Type stringType = typeof(string);
            Type intType = typeof(int);
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
                    Expression.Convert(commandKeyParameter, stringType),
                    Expression.Convert(groupKeyParameter, stringType),
                    Expression.Convert(domainParameter, stringType),
                    Expression.Convert(maxConcurrentCountParameter, intType)
                });
            return Expression.Lambda<Action<string, string, string, int>>(
                callMethod, commandKeyParameter, groupKeyParameter, domainParameter, maxConcurrentCountParameter).Compile();
        }

        private static Func<string, object> UtilsSemaphoreIsolationMakeCreateInstanceDelegate(Type utilsSemaphoreIsolationType)
        {
            Type stringType = typeof(string);
            var commandKeyParameter = Expression.Parameter(stringType, "commandKey");
            Expression callMethod = Expression.Call(
                utilsSemaphoreIsolationType,
                CHystrixUtilsSemaphoreIsolationCreateInstanceMethodName,
                new Type[] { },
                new Expression[]
                {
                    Expression.Convert(commandKeyParameter, stringType)
                });
            return Expression.Lambda<Func<string, object>>(callMethod, commandKeyParameter).Compile();
        }

        private static Action<object> UtilsSemaphoreIsolationMakeStartExecutionDelegate(Type utilsSemaphoreIsolationType)
        {
            return UtilsSemaphoreIsolationMakeExecutionMethodDelegate(utilsSemaphoreIsolationType, CHystrixUtilsSemaphoreIsolationStartExecutionMethodName);
        }

        private static Action<object> UtilsSemaphoreIsolationMakeMarkSuccessDelegate(Type utilsSemaphoreIsolationType)
        {
            return UtilsSemaphoreIsolationMakeExecutionMethodDelegate(utilsSemaphoreIsolationType, CHystrixUtilsSemaphoreIsolationMarkSuccessMethodName);
        }

        private static Action<object> UtilsSemaphoreIsolationMakeMarkFailureDelegate(Type utilsSemaphoreIsolationType)
        {
            return UtilsSemaphoreIsolationMakeExecutionMethodDelegate(utilsSemaphoreIsolationType, CHystrixUtilsSemaphoreIsolationMarkFailureMethodName);
        }

        private static Action<object> UtilsSemaphoreIsolationMakeMarkBadRequestDelegate(Type utilsSemaphoreIsolationType)
        {
            return UtilsSemaphoreIsolationMakeExecutionMethodDelegate(utilsSemaphoreIsolationType, CHystrixUtilsSemaphoreIsolationMarkBadRequestMethodName);
        }

        private static Action<object> UtilsSemaphoreIsolationMakeEndExecutionDelegate(Type utilsSemaphoreIsolationType)
        {
            return UtilsSemaphoreIsolationMakeExecutionMethodDelegate(utilsSemaphoreIsolationType, CHystrixUtilsSemaphoreIsolationEndExecutionMethodName);
        }

        private static Action<object> UtilsSemaphoreIsolationMakeExecutionMethodDelegate(Type utilsSemaphoreIsolationType, string methodName)
        {
            Type objectType = typeof(object);
            var instanceParameter = Expression.Parameter(objectType, "instance");
            Expression callMethod = Expression.Call(
                utilsSemaphoreIsolationType,
                methodName,
                new Type[] { },
                new Expression[]
                {
                    Expression.Convert(instanceParameter, objectType)
                });
            return Expression.Lambda<Action<object>>(callMethod, instanceParameter).Compile();
        }
    }
}
