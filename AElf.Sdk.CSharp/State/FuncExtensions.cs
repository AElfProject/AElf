using System;
using System.Collections.Generic;

namespace AElf.Sdk.CSharp.State
{
    public static class FuncExtensions
    {
        public static readonly HashSet<Type> Funcs = new HashSet<Type>()
        {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,,>)
        };

        public static bool IsFunc(this Type type)
        {
            if (type.IsConstructedGenericType)
            {
                return Funcs.Contains(type.GetGenericTypeDefinition());
            }

            return Funcs.Contains(type);
        }
    }
}