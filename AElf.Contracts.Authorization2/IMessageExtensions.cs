using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Contracts.Authorization2
{
    public static class IMessageExtensions
    {
        private static readonly Dictionary<Type, object> _empties = new Dictionary<Type, object>();

        private static object GetEmpty(IMessage message)
        {
            var type = message.GetType();
            if (_empties.TryGetValue(type, out var empty))
            {
                return empty;
            }

            empty = Activator.CreateInstance(type);
            _empties.Add(type, empty);
            return empty;
        }

        public static bool IsEmpty(this IMessage message)
        {
            return message.Equals(GetEmpty(message));
        }

        public static bool IsNotEmpty(this IMessage message)
        {
            return !message.IsEmpty();
        }
    }
}