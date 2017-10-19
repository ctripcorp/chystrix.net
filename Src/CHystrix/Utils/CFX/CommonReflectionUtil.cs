using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using System.Reflection.Emit;

namespace CHystrix.Utils.CFX
{
    [SecurityCritical]
    internal static class CommonReflectionUtil
    {
        public static void Assert(bool b)
        {
            if (!b)
            {
                throw new PlatformNotSupportedException();
            }
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public static TDelegate BindMethodToDelegate<TDelegate>(MethodInfo methodInfo) where TDelegate : class
        {
            Type[] typeArray;
            Type type;
            ExtractDelegateSignature(typeof(TDelegate), out typeArray, out type);
            string name = "BindMethodToDelegate_" + methodInfo.Name;
            Type returnType = type;
            Type[] parameterTypes = typeArray;
            bool restrictedSkipVisibility = true;
            DynamicMethod method = new DynamicMethod(name, returnType, parameterTypes, restrictedSkipVisibility);
            ILGenerator iLGenerator = method.GetILGenerator();
            for (int i = 0; i < typeArray.Length; i++)
            {
                iLGenerator.Emit(OpCodes.Ldarg, (short)i);
            }
            iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
            iLGenerator.Emit(OpCodes.Ret);
            return (method.CreateDelegate(typeof(TDelegate)) as TDelegate);
        }

        private static void ExtractDelegateSignature(Type delegateType, out Type[] argumentTypes, out Type returnType)
        {
            MethodInfo method = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            argumentTypes = Array.ConvertAll<ParameterInfo, Type>(method.GetParameters(), pInfo => pInfo.ParameterType);
            returnType = method.ReturnType;
        }

        public static ConstructorInfo FindConstructor(Type type, bool isStatic, Type[] argumentTypes)
        {
            ConstructorInfo info = type.GetConstructor(GetBindingFlags(isStatic), null, argumentTypes, null);
            Assert(info != null);
            return info;
        }

        public static FieldInfo FindField(Type containingType, string fieldName, bool isStatic, Type fieldType)
        {
            FieldInfo field = containingType.GetField(fieldName, GetBindingFlags(isStatic));
            Assert(field.FieldType == fieldType);
            return field;
        }

        public static MethodInfo FindMethod(Type containingType, string methodName, bool isStatic, Type[] argumentTypes, Type returnType)
        {
            MethodInfo info = containingType.GetMethod(methodName, GetBindingFlags(isStatic), null, argumentTypes, null);
            Assert(info.ReturnType == returnType);
            return info;
        }

        private static BindingFlags GetBindingFlags(bool isStatic)
        {
            return (((isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic) | BindingFlags.Public);
        }

        public static T MakeDelegate<T>(MethodInfo method) where T : class
        {
            return MakeDelegate<T>(null, method);
        }

        public static T MakeDelegate<T>(object target, MethodInfo method) where T : class
        {
            return (MakeDelegate(typeof(T), target, method) as T);
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public static Delegate MakeDelegate(Type delegateType, object target, MethodInfo method)
        {
            return Delegate.CreateDelegate(delegateType, target, method);
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public static Func<TInstance, TDelegate> MakeFastCreateDelegate<TInstance, TDelegate>(MethodInfo methodInfo)
            where TInstance : class
            where TDelegate : class
        {
            string name = "FastCreateDelegate_" + methodInfo.Name;
            Type returnType = typeof(TDelegate);
            Type[] parameterTypes = new Type[] { typeof(TInstance) };
            bool restrictedSkipVisibility = true;
            DynamicMethod method = new DynamicMethod(name, returnType, parameterTypes, restrictedSkipVisibility);
            ConstructorInfo constructor = typeof(TDelegate).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });
            ILGenerator iLGenerator = method.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Dup);
            iLGenerator.Emit(OpCodes.Ldvirtftn, methodInfo);
            iLGenerator.Emit(OpCodes.Newobj, constructor);
            iLGenerator.Emit(OpCodes.Ret);
            return (Func<TInstance, TDelegate>)method.CreateDelegate(typeof(Func<TInstance, TDelegate>));
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public static TDelegate MakeFastNewObject<TDelegate>(Type type) where TDelegate : class
        {
            Type[] typeArray;
            Type type2;
            ExtractDelegateSignature(typeof(TDelegate), out typeArray, out type2);
            Type type3 = type;
            bool isStatic = false;
            Type[] argumentTypes = typeArray;
            ConstructorInfo con = FindConstructor(type3, isStatic, argumentTypes);
            string name = "MakeFastNewObject_" + type.Name;
            Type returnType = type2;
            Type[] parameterTypes = typeArray;
            bool restrictedSkipVisibility = true;
            DynamicMethod method = new DynamicMethod(name, returnType, parameterTypes, restrictedSkipVisibility);
            ILGenerator iLGenerator = method.GetILGenerator();
            for (int i = 0; i < typeArray.Length; i++)
            {
                iLGenerator.Emit(OpCodes.Ldarg, (short)i);
            }
            iLGenerator.Emit(OpCodes.Newobj, con);
            iLGenerator.Emit(OpCodes.Ret);
            return (method.CreateDelegate(typeof(TDelegate)) as TDelegate);
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public static object ReadField(FieldInfo fieldInfo, object target)
        {
            return fieldInfo.GetValue(target);
        }

        [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
        public static void WriteField(FieldInfo fieldInfo, object target, object value)
        {
            fieldInfo.SetValue(target, value);
        }
    }
}
