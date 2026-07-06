using System;
using System.Collections.Generic;
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

            MethodInfo method = ResolveMethod(
                target.GetType(),
                methodName,
                InstanceMemberFlags,
                args,
                out object[] invocationArgs);
            object result = method.Invoke(target, invocationArgs);
            CopyBackByRefArguments(method, invocationArgs, args);
            return result;
        }

        public object InvokeStatic(Type type, string methodName, params object[] args)
        {
            Assert.That(type, Is.Not.Null, $"Cannot invoke static method {methodName} on a null type.");

            MethodInfo method = ResolveMethod(
                type,
                methodName,
                StaticMemberFlags,
                args,
                out object[] invocationArgs);
            object result = method.Invoke(null, invocationArgs);
            CopyBackByRefArguments(method, invocationArgs, args);
            return result;
        }

        public object GetStaticProperty(Type type, string propertyName)
        {
            Assert.That(type, Is.Not.Null, $"Cannot read static property {propertyName} from a null type.");

            PropertyInfo property = type.GetProperty(propertyName, StaticMemberFlags);
            Assert.That(
                property,
                Is.Not.Null,
                $"Static property not found: {type.FullName}.{propertyName}");
            return property.GetValue(null);
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

        private static MethodInfo ResolveMethod(
            Type type,
            string methodName,
            BindingFlags flags,
            object[] args,
            out object[] invocationArgs)
        {
            object[] providedArgs = args ?? new object[0];
            List<MethodMatch> matches = new List<MethodMatch>();
            MethodInfo[] methods = type.GetMethods(flags);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName || method.ContainsGenericParameters)
                    continue;

                if (!TryBuildInvocationArguments(
                        method.GetParameters(),
                        providedArgs,
                        out object[] candidateArgs,
                        out int score))
                {
                    continue;
                }

                matches.Add(new MethodMatch(method, candidateArgs, score));
            }

            if (matches.Count == 0)
            {
                Assert.Fail(
                    $"Method not found: {type.FullName}.{methodName}({FormatArgumentTypes(providedArgs)})");
            }

            matches.Sort((left, right) => left.Score.CompareTo(right.Score));
            if (matches.Count > 1 && matches[0].Score == matches[1].Score)
            {
                Assert.Fail(
                    $"Ambiguous method match: {type.FullName}.{methodName}({FormatArgumentTypes(providedArgs)}). " +
                    $"Candidates: {FormatMethodMatches(matches, matches[0].Score)}");
            }

            invocationArgs = matches[0].InvocationArgs;
            return matches[0].Method;
        }

        private static bool TryBuildInvocationArguments(
            ParameterInfo[] parameters,
            object[] providedArgs,
            out object[] invocationArgs,
            out int score)
        {
            invocationArgs = null;
            score = 0;

            if (providedArgs.Length > parameters.Length)
                return false;

            object[] candidateArgs = new object[parameters.Length];
            for (int i = 0; i < providedArgs.Length; i++)
            {
                if (!CanAssignArgument(
                        providedArgs[i],
                        parameters[i],
                        out int argumentScore))
                {
                    return false;
                }

                candidateArgs[i] = providedArgs[i];
                score += argumentScore;
            }

            for (int i = providedArgs.Length; i < parameters.Length; i++)
            {
                if (!parameters[i].IsOptional)
                    return false;

                candidateArgs[i] = Type.Missing;
                score += 10;
            }

            invocationArgs = candidateArgs;
            return true;
        }

        private static bool CanAssignArgument(object arg, ParameterInfo parameter, out int score)
        {
            Type parameterType = parameter.ParameterType;
            Type effectiveParameterType = parameterType.IsByRef
                ? parameterType.GetElementType()
                : parameterType;

            if (arg == null)
            {
                score = 3;
                if (parameterType.IsByRef && parameter.IsOut)
                    return true;

                return !effectiveParameterType.IsValueType ||
                    Nullable.GetUnderlyingType(effectiveParameterType) != null;
            }

            Type argumentType = arg.GetType();
            if (effectiveParameterType == argumentType)
            {
                score = 0;
                return true;
            }

            Type nullableType = Nullable.GetUnderlyingType(effectiveParameterType);
            if (nullableType != null && nullableType == argumentType)
            {
                score = 1;
                return true;
            }

            if (effectiveParameterType.IsAssignableFrom(argumentType))
            {
                score = 2;
                return true;
            }

            score = int.MaxValue;
            return false;
        }

        private static void CopyBackByRefArguments(
            MethodInfo method,
            object[] invocationArgs,
            object[] originalArgs)
        {
            if (originalArgs == null || originalArgs.Length == 0)
                return;

            ParameterInfo[] parameters = method.GetParameters();
            int count = Math.Min(originalArgs.Length, parameters.Length);
            for (int i = 0; i < count; i++)
            {
                if (!parameters[i].ParameterType.IsByRef)
                    continue;

                originalArgs[i] = invocationArgs[i];
            }
        }

        private static string FormatMethodMatches(List<MethodMatch> matches, int score)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Score != score)
                    continue;

                names.Add(FormatMethod(matches[i].Method));
            }

            return string.Join("; ", names);
        }

        private static string FormatMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type[] parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                parameterTypes[i] = parameters[i].ParameterType;

            return $"{method.Name}({FormatTypes(parameterTypes)})";
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

        private readonly struct MethodMatch
        {
            public MethodMatch(MethodInfo method, object[] invocationArgs, int score)
            {
                Method = method;
                InvocationArgs = invocationArgs;
                Score = score;
            }

            public MethodInfo Method { get; }
            public object[] InvocationArgs { get; }
            public int Score { get; }
        }
    }
}
