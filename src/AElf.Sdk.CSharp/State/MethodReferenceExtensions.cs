using System;

namespace AElf.Sdk.CSharp.State
{
    public static class MethodReferenceExtensions
    {
        public static bool IsMethodReference(this Type type)
        {
            if (type.IsConstructedGenericType)
            {
                return type.GetGenericTypeDefinition() == typeof(MethodReference<,>);
            }

            return type == typeof(MethodReference<,>);
        }
    }
}