using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests.TestSupport.Core
{
    internal sealed class ReflectionTestAccess
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const BindingFlags StaticMemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public Type RequireType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName, false);
            Assert.That(type, Is.Not.Null, $"Type not found: {assemblyQualifiedName}");
            return type;
        }

        public object CreateInstance(Type type, params object[] args)
        {
            Assert.That(type, Is.Not.Null, "Cannot create an instance of a null type.");

            object[] invocationArgs = args ?? new object[0];
            object instance = invocationArgs.Length == 0
                ? Activator.CreateInstance(type)
                : Activator.CreateInstance(type, invocationArgs);

            Assert.That(instance, Is.Not.Null, $"Failed to create instance: {type.FullName}");
            return instance;
        }

        public object Invoke(object target, string methodName, params object[] args)
        {
            Assert.That(target, Is.Not.Null, $"Cannot invoke {methodName} on a null target.");

            MethodInfo method = target.GetType().GetMethod(methodName, InstanceMemberFlags);
            Assert.That(
                method,
                Is.Not.Null,
                $"Method not found: {target.GetType().FullName}.{methodName}({FormatArgumentTypes(args)})");
            return method.Invoke(target, args);
        }

        public object InvokeStatic(Type type, string methodName, params object[] args)
        {
            Assert.That(type, Is.Not.Null, $"Cannot invoke static method {methodName} on a null type.");

            MethodInfo method = type.GetMethod(methodName, StaticMemberFlags);
            Assert.That(
                method,
                Is.Not.Null,
                $"Static method not found: {type.FullName}.{methodName}({FormatArgumentTypes(args)})");
            return method.Invoke(null, args);
        }

        public object InvokeWithSignature(
            object target,
            string methodName,
            Type[] parameterTypes,
            params object[] args)
        {
            Assert.That(target, Is.Not.Null, $"Cannot invoke {methodName} on a null target.");
            Assert.That(parameterTypes, Is.Not.Null, $"Parameter types are required for {methodName}.");

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                InstanceMemberFlags,
                binder: null,
                types: parameterTypes,
                modifiers: null);
            Assert.That(
                method,
                Is.Not.Null,
                $"Method not found: {target.GetType().FullName}.{methodName}({FormatTypes(parameterTypes)})");
            return method.Invoke(target, args);
        }

        public object GetProperty(object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null, $"Cannot read property {propertyName} from a null target.");

            PropertyInfo property = target.GetType().GetProperty(propertyName, InstanceMemberFlags);
            Assert.That(
                property,
                Is.Not.Null,
                $"Property not found: {target.GetType().FullName}.{propertyName}");
            return property.GetValue(target);
        }

        public void SetProperty(object target, string propertyName, object value)
        {
            Assert.That(target, Is.Not.Null, $"Cannot set property {propertyName} on a null target.");

            PropertyInfo property = target.GetType().GetProperty(propertyName, InstanceMemberFlags);
            Assert.That(
                property,
                Is.Not.Null,
                $"Property not found: {target.GetType().FullName}.{propertyName}");
            property.SetValue(target, value);
        }

        public object GetPrivateField(object target, string fieldName)
        {
            Assert.That(target, Is.Not.Null, $"Cannot read field {fieldName} from a null target.");

            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(
                field,
                Is.Not.Null,
                $"Private field not found: {target.GetType().FullName}.{fieldName}");
            return field.GetValue(target);
        }

        public void SetPrivateField(object target, string fieldName, object value)
        {
            Assert.That(target, Is.Not.Null, $"Cannot set field {fieldName} on a null target.");

            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(
                field,
                Is.Not.Null,
                $"Private field not found: {target.GetType().FullName}.{fieldName}");
            field.SetValue(target, value);
        }

        private static string FormatArgumentTypes(object[] args)
        {
            if (args == null || args.Length == 0)
                return string.Empty;

            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
                types[i] = args[i] == null ? null : args[i].GetType();

            return FormatTypes(types);
        }

        private static string FormatTypes(Type[] types)
        {
            if (types == null || types.Length == 0)
                return string.Empty;

            string[] names = new string[types.Length];
            for (int i = 0; i < types.Length; i++)
                names[i] = types[i] == null ? "null" : types[i].FullName;

            return string.Join(", ", names);
        }
    }
}
