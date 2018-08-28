using System;
using System.Linq;
using Google.Protobuf;

namespace AElf.Kernel.Types
{
    public static class SerializableTypeHelper
    {
        public static T Deserialize<T>(this byte[] bytes) where T :  IMessage, new ()
        {
            if (typeof(T).GetInterfaces().Contains(typeof(IMessage)))
            {
                var obj = new T();
                ((IMessage)obj).MergeFrom(bytes);
                return obj;
            }

            throw new Exception($"Not Serializable Type, {typeof(T).FullName}");
        }
       
    }
}