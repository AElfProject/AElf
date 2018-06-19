using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public static class TypeCheckExtension
    {
        private static HashSet<Type> _allowedPrimitives = new HashSet<System.Type>()
        {
            typeof(void), typeof(bool), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(string), typeof(byte[])
        };

        public static bool IsAllowedType(this Type type)
        {
            return type.IsAllowedPrimitiveType() || type.IsPbMessageType() || type.IsUserType();
        }

        public static bool IsAllowedPrimitiveType(this Type type)
        {
            return _allowedPrimitives.Contains(type);
        }

        public static bool IsPbMessageType(this Type type)
        {
            return typeof(IMessage).IsAssignableFrom(type);
        }

        public static bool IsUserType(this Type type)
        {
            return type.IsSubclassOf(typeof(UserType));
        }
    }
}
