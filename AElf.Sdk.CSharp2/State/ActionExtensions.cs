using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AElf.Sdk.CSharp.State;

namespace AElf.Sdk.CSharp
{
    public static class ActionExtensions
    {
        public static readonly HashSet<Type> Actions = new HashSet<Type>()
        {
            typeof(Action),
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>),
            typeof(Action<,,,,>),
            typeof(Action<,,,,,>),
            typeof(Action<,,,,,,>),
            typeof(Action<,,,,,,,>),
            typeof(Action<,,,,,,,,>),
            typeof(Action<,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,,>)
        };

        public static bool IsAction(this Type type)
        {
            if (type.IsConstructedGenericType)
            {
                return Actions.Contains(type.GetGenericTypeDefinition());
            }

            return Actions.Contains(type);
        }
    }
}