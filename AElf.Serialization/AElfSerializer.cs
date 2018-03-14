using System;
using System.Collections.Generic;
using AElf.Serialization.Protobuf;
using AElf.Serialization.Protobuf.Generated;

namespace AElf.Serialization
{
    public class AElfSerializer: IAElfSerializer
    {
        private Dictionary<Type, Type> _types=new Dictionary<Type, Type>()
        {
            {typeof(ITestAccount),typeof(ProtoAccount)}
        };


        private ProtobufSerializer _protobufSerializer;

        public AElfSerializer(ProtobufSerializer protobufSerializer)
        {
            _protobufSerializer = protobufSerializer;
        }

        public byte[] Serialize(object obj)
        {
            return _protobufSerializer.Serialize(obj);
            
        }

        public T Deserialize<T>(byte[] bytes) where T : class
        {
            return (T) Deserialize(bytes, typeof(T));
        }

        public object Deserialize(byte[] bytes, Type type)
        {
            var t = _types[type];
            return _protobufSerializer.Deserialize(bytes, t);
        }
    }
}