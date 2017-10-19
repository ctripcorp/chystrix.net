using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Web.Routing;
using System.ComponentModel;
using System.Threading;
using System.IO;

using CHystrix.Utils;
using CHystrix.Utils.CFX;

namespace CHystrix.Web
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Initializer
    {
        static volatile bool _done;
        static object _lock = new object();
        static Action<Type> _registerModuleMethodFromReflection;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void PreApplicationStartCode()
        {
            if (_done)
                return;

            if (!Monitor.TryEnter(_lock))
                return;

            try
            {
                if (_done)
                    return;
                _done = true;

                RegisterHystrixRoutes();

                if (HostingEnvironment.IsHosted)
                {
                    PreRegisterModule();
                    RegisterModule(typeof(HystrixModule));
                }
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Fatal, "CHystrix Web Initializer failed at startup.", ex,
                    new Dictionary<string,string>().AddLogTagData("FXD303028"));
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        private static void RegisterHystrixRoutes()
        {
            RouteTable.Routes.Add(
                "chystrix-rdkjsoa2",
                new Route(HystrixRouteHandler.Route, new HystrixRouteHandler())
                {
                    Constraints = new RouteValueDictionary()
                    {
                        { HystrixRouteHandler.ControllerVariable, HystrixRouteHandler.HystrixRoutePrefix }
                    }
                });
        }

        private static void PreRegisterModule()
        {
            Assembly assembly = GetAssembly("Arch.CFX");
            if (assembly != null)
            {
                Type type = assembly.GetType("Arch.CFramework.InnerAppInternals");
                if (type != null)
                {
                    MethodInfo method = type.GetMethod("RegisterModule");
                    if (method != null)
                    {
                        _registerModuleMethodFromReflection = Delegate.CreateDelegate(typeof(Action<Type>), method) as Action<Type>;
                        return;
                    }
                }
            }

            assembly = GetAssembly("Microsoft.Web.Infrastructure");
            if (assembly != null)
            {
                Type type = assembly.GetType("Microsoft.Web.Infrastructure.DynamicModuleHelper.DynamicModuleUtility");
                if (type != null)
                {
                    MethodInfo method = type.GetMethod("RegisterModule");
                    if (method != null)
                    {
                        _registerModuleMethodFromReflection = Delegate.CreateDelegate(typeof(Action<Type>), method) as Action<Type>;
                        return;
                    }
                }
            }

            _registerModuleMethodFromReflection = DynamicModuleUtility.RegisterModule;
        }

        private static void RegisterModule(Type type)
        {
            _registerModuleMethodFromReflection(type);
        }

        private static Assembly GetAssembly(string assemblyName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }

            string assemblyFilePath = null;
            try
            {
                assemblyFilePath = HttpRuntime.BinDirectory + assemblyName + ".dll";
                if (!File.Exists(assemblyFilePath))
                {
                    CommonUtils.Log.Log(LogLevelEnum.Info, "No assembly " + assemblyFilePath + " exists.",
                        new Dictionary<string,string>().AddLogTagData("FXD303048"));
                    return null;
                }
                Assembly assembly = Assembly.LoadFrom(assemblyFilePath);
                CommonUtils.Log.Log(LogLevelEnum.Info, "Loaded assembly " + assemblyFilePath,
                    new Dictionary<string,string>().AddLogTagData("FXD303049"));
                return assembly;
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Info, "Load assembly " + (assemblyFilePath ?? assemblyName) + " failed.", ex,
                    new Dictionary<string,string>().AddLogTagData("FXD303050"));
            }

            return null;
        }
    }
}
