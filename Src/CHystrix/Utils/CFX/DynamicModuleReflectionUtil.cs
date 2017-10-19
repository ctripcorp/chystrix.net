using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Reflection;
using System.Web;
using System.Collections;
using System.Web.Configuration;
using System.Web.Compilation;
using System.Security.Permissions;
using System.Configuration;

namespace CHystrix.Utils.CFX
{
    [SecurityCritical]
    internal sealed class DynamicModuleReflectionUtil
    {
        private ConstructorInfo _ci_ListOfModuleConfigurationInfo;
        private ConstructorInfo _ci_ModuleConfigurationInfo;
        private FieldInfo _fi_ConfigurationElementCollection_bReadOnly;
        private FieldInfo _fi_HttpApplication_moduleCollection;
        private FieldInfo _fi_HttpApplication_moduleConfigInfo;
        private FieldInfo _fi_PipelineRuntime_s_ApplicationContext;
        private MethodInfo _mi_HttpApplication_BuildIntegratedModuleCollection;
        private MethodInfo _mi_HttpApplication_GetModuleCollection;
        private MethodInfo _mi_HttpModuleCollection_AddModule;
        private MethodInfo _mi_RuntimeConfig_getHttpModules;
        private Type _type_ListOfModuleConfigurationInfo;
        public static readonly Action<Type> Fx45RegisterModuleDelegate = GetFx45RegisterModuleDelegate();
        public static readonly DynamicModuleReflectionUtil Instance = GetInstance();

        private DynamicModuleReflectionUtil()
        {
        }

        public void AddModuleToCollection(HttpModuleCollection target, string name, IHttpModule m)
        {
            CommonReflectionUtil.MakeDelegate<Action<string, IHttpModule>>(target, this._mi_HttpModuleCollection_AddModule)(name, m);
        }

        public HttpModuleCollection BuildIntegratedModuleCollection(HttpApplication target, IList moduleList)
        {
            return (HttpModuleCollection)CommonReflectionUtil.MakeDelegate(typeof(Func<,>).MakeGenericType(new Type[] { this._type_ListOfModuleConfigurationInfo, typeof(HttpModuleCollection) }), target, this._mi_HttpApplication_BuildIntegratedModuleCollection).DynamicInvoke(new object[] { moduleList });
        }

        private static Action<Type> GetFx45RegisterModuleDelegate()
        {
            MethodInfo method = typeof(HttpApplication).GetMethod("RegisterModule", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Type) }, null);
            if (method == null)
            {
                return null;
            }
            return (Action<Type>)Delegate.CreateDelegate(typeof(Action<Type>), method);
        }

        public HttpModulesSection GetHttpModulesFromAppConfig(object target)
        {
            return CommonReflectionUtil.MakeDelegate<Func<HttpModulesSection>>(target, this._mi_RuntimeConfig_getHttpModules)();
        }

        private static DynamicModuleReflectionUtil GetInstance()
        {
            try
            {
                if (Fx45RegisterModuleDelegate != null)
                {
                    return null;
                }
                DynamicModuleReflectionUtil util = new DynamicModuleReflectionUtil();
                MethodInfo method = typeof(BuildManager).GetMethod("ThrowIfPreAppStartNotRunning", BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
                util.ThrowIfPreAppStartNotRunning = CommonReflectionUtil.MakeDelegate<Action>(method);
                CommonReflectionUtil.Assert(util.ThrowIfPreAppStartNotRunning != null);
                Type type = CommonAssemblies.SystemWeb.GetType("System.Web.Configuration.RuntimeConfig");
                MethodInfo info2 = type.GetMethod("GetAppConfig", BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
                util.GetAppConfig = CommonReflectionUtil.MakeDelegate<Func<object>>(info2);
                CommonReflectionUtil.Assert(util.GetAppConfig != null);
                Type containingType = type;
                string methodName = "get_HttpModules";
                bool isStatic = false;
                Type[] emptyTypes = Type.EmptyTypes;
                Type returnType = typeof(HttpModulesSection);
                util._mi_RuntimeConfig_getHttpModules = CommonReflectionUtil.FindMethod(containingType, methodName, isStatic, emptyTypes, returnType);
                Type type7 = typeof(ConfigurationElementCollection);
                string fieldName = "bReadOnly";
                bool flag2 = false;
                Type fieldType = typeof(bool);
                util._fi_ConfigurationElementCollection_bReadOnly = CommonReflectionUtil.FindField(type7, fieldName, flag2, fieldType);
                Type type2 = CommonAssemblies.SystemWeb.GetType("System.Web.ModuleConfigurationInfo");
                Type type9 = type2;
                bool flag3 = false;
                Type[] argumentTypes = new Type[] { typeof(string), typeof(string), typeof(string) };
                util._ci_ModuleConfigurationInfo = CommonReflectionUtil.FindConstructor(type9, flag3, argumentTypes);
                Type type3 = typeof(List<>).MakeGenericType(new Type[] { type2 });
                util._type_ListOfModuleConfigurationInfo = type3;
                Type type10 = type3;
                bool flag4 = false;
                Type[] typeArray5 = Type.EmptyTypes;
                util._ci_ListOfModuleConfigurationInfo = CommonReflectionUtil.FindConstructor(type10, flag4, typeArray5);
                Type type11 = typeof(HttpApplication);
                string str3 = "_moduleConfigInfo";
                bool flag5 = true;
                Type type12 = type3;
                util._fi_HttpApplication_moduleConfigInfo = CommonReflectionUtil.FindField(type11, str3, flag5, type12);
                Type type13 = typeof(HttpApplication);
                string str4 = "_moduleCollection";
                bool flag6 = false;
                Type type14 = typeof(HttpModuleCollection);
                util._fi_HttpApplication_moduleCollection = CommonReflectionUtil.FindField(type13, str4, flag6, type14);
                Type type4 = CommonAssemblies.SystemWeb.GetType("System.Web.Hosting.PipelineRuntime");
                Type type15 = type4;
                string str5 = "s_ApplicationContext";
                bool flag7 = true;
                Type type16 = typeof(IntPtr);
                util._fi_PipelineRuntime_s_ApplicationContext = CommonReflectionUtil.FindField(type15, str5, flag7, type16);
                Type type17 = typeof(HttpApplication);
                string str6 = "GetModuleCollection";
                bool flag8 = false;
                Type[] typeArray7 = new Type[] { typeof(IntPtr) };
                Type type18 = typeof(HttpModuleCollection);
                util._mi_HttpApplication_GetModuleCollection = CommonReflectionUtil.FindMethod(type17, str6, flag8, typeArray7, type18);
                Type type19 = typeof(HttpApplication);
                string str7 = "BuildIntegratedModuleCollection";
                bool flag9 = false;
                Type[] typeArray9 = new Type[] { type3 };
                Type type20 = typeof(HttpModuleCollection);
                util._mi_HttpApplication_BuildIntegratedModuleCollection = CommonReflectionUtil.FindMethod(type19, str7, flag9, typeArray9, type20);
                Type type21 = typeof(HttpModuleCollection);
                string str8 = "AddModule";
                bool flag10 = false;
                Type[] typeArray11 = new Type[] { typeof(string), typeof(IHttpModule) };
                Type type22 = typeof(void);
                util._mi_HttpModuleCollection_AddModule = CommonReflectionUtil.FindMethod(type21, str8, flag10, typeArray11, type22);
                return util;
            }
            catch
            {
                return null;
            }
        }

        public IntPtr GetIntegratedModeContext()
        {
            return (IntPtr)CommonReflectionUtil.ReadField(this._fi_PipelineRuntime_s_ApplicationContext, null);
        }

        public HttpModuleCollection GetIntegratedModuleCollection(HttpApplication target, IntPtr appContext)
        {
            return CommonReflectionUtil.MakeDelegate<Func<IntPtr, HttpModuleCollection>>(target, this._mi_HttpApplication_GetModuleCollection)(appContext);
        }

        public IList GetModuleConfigInfo()
        {
            return (IList)CommonReflectionUtil.ReadField(this._fi_HttpApplication_moduleConfigInfo, null);
        }

        public HttpModuleCollection GetRegisteredModuleCollection(HttpApplication target)
        {
            return (HttpModuleCollection)CommonReflectionUtil.ReadField(this._fi_HttpApplication_moduleCollection, target);
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public IList NewListOfModuleConfigurationInfo()
        {
            return (IList)this._ci_ListOfModuleConfigurationInfo.Invoke(null);
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public object NewModuleConfigurationInfo(string name, string type, string condition)
        {
            return this._ci_ModuleConfigurationInfo.Invoke(new object[] { name, type, condition });
        }

        public void SetConfigurationElementCollectionReadOnlyBit(ConfigurationElementCollection target, bool value)
        {
            CommonReflectionUtil.WriteField(this._fi_ConfigurationElementCollection_bReadOnly, target, value);
        }

        public void SetModuleConfigInfo(IList value)
        {
            CommonReflectionUtil.WriteField(this._fi_HttpApplication_moduleConfigInfo, null, value);
        }

        public Func<object> GetAppConfig { get; private set; }

        public Action ThrowIfPreAppStartNotRunning { get; private set; }
    }
}
