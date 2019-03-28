using System;
using System.Collections.Generic;

namespace AElf.Sdk.CSharp
{
    //TODO: AElf.Sdk.CSharp State neeed add test cases [Case]
    public static class ActionExtensions
    {
        public static readonly HashSet<Type> Actions = new HashSet<Type>
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
            if (type.IsConstructedGenericType) return Actions.Contains(type.GetGenericTypeDefinition());

            return Actions.Contains(type);
        }
    }
}