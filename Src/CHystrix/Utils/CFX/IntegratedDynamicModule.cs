using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Security;
using System.Threading;
using System.Collections;

namespace CHystrix.Utils.CFX
{
    internal sealed class IntegratedDynamicModule : IHttpModule
    {
        internal const string ModuleName = "__ASP_IntegratedDynamicModule_Shim";

        private IntegratedDynamicModule()
        {
        }

        public void Dispose()
        {
        }

        [SecuritySafeCritical]
        public void Init(HttpApplication context)
        {
            CriticalStatics.Init(context);
        }

        [SecurityCritical]
        internal static class CriticalStatics
        {
            internal static int _hasBeenInitialized = 0;
            internal static readonly DynamicModuleReflectionUtil _reflectionUtil = DynamicModuleReflectionUtil.Instance;
            internal static readonly List<DynamicModuleRegistryEntry> DynamicEntries = new List<DynamicModuleRegistryEntry>();

            public static void Init(HttpApplication context)
            {
                if ((Interlocked.Exchange(ref _hasBeenInitialized, 1) != 1) && ((DynamicEntries.Count != 0) && (_reflectionUtil != null)))
                {
                    IntPtr integratedModeContext = _reflectionUtil.GetIntegratedModeContext();
                    if (integratedModeContext != IntPtr.Zero)
                    {
                        _reflectionUtil.SetModuleConfigInfo(null);
                        HttpModuleCollection integratedModuleCollection = _reflectionUtil.GetIntegratedModuleCollection(context, integratedModeContext);
                        IList moduleConfigInfo = _reflectionUtil.GetModuleConfigInfo();
                        string name = "__ASP_IntegratedDynamicModule_Shim";
                        string assemblyQualifiedName = typeof(IntegratedDynamicModule).AssemblyQualifiedName;
                        string condition = "managedHandler";
                        moduleConfigInfo.Insert(0, _reflectionUtil.NewModuleConfigurationInfo(name, assemblyQualifiedName, condition));
                        foreach (DynamicModuleRegistryEntry entry in DynamicEntries)
                        {
                            moduleConfigInfo.Add(_reflectionUtil.NewModuleConfigurationInfo(entry.Name, entry.Type, "managedHandler"));
                        }
                        HttpModuleCollection registeredModuleCollection = _reflectionUtil.GetRegisteredModuleCollection(context);
                        for (int i = 0; i < integratedModuleCollection.Count; i++)
                        {
                            _reflectionUtil.AddModuleToCollection(registeredModuleCollection, integratedModuleCollection.GetKey(i), integratedModuleCollection.Get(i));
                        }
                        IList moduleList = _reflectionUtil.NewListOfModuleConfigurationInfo();
                        for (int j = moduleConfigInfo.Count - DynamicEntries.Count; j < moduleConfigInfo.Count; j++)
                        {
                            moduleList.Add(moduleConfigInfo[j]);
                        }
                        HttpModuleCollection modules3 = _reflectionUtil.BuildIntegratedModuleCollection(context, moduleList);
                        for (int k = 0; k < modules3.Count; k++)
                        {
                            _reflectionUtil.AddModuleToCollection(registeredModuleCollection, modules3.GetKey(k), modules3.Get(k));
                        }
                    }
                }
            }
        }
    }
}
