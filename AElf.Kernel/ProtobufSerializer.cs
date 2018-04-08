using System;
using System.Collections.Concurrent;
using Google.Protobuf;

namespace AElf.Kernel
{
    /// <summary>
    /// Protobuf serializer. This uses the serialization code
    /// built into the generated protobuf types
    /// </summary>
    public class ProtobufSerializer 
    {
        /// <summary>
        /// Cache that stores the protobuf types. It's needed to avoid
        /// instantiating the protobuf objects every time.
        /// The key is the protobuf generated class' full name.
        /// The value is the corresponding parser.
        /// </summary>
        private static readonly ConcurrentDictionary<string, MessageParser> TypeCache
            = new ConcurrentDictionary<string, MessageParser>();

        /// <summary>
        /// This just wrapps around protobufs extension method.
        /// </summary>
        public byte[] Serialize(object obj)
        {
            IMessage message = obj as IMessage;
            return message?.ToByteArray();
        }

        /// <summary>
        /// Helper method to perform the cast. Note that you
        /// cannot use this method to deserialize structs.
        /// </summary>
        public T Deserialize<T>(byte[] bytes) where T : class
        {
            var data = Deserialize(bytes, typeof(T));
            return data != null ? (T)data : null;
        }

        /// <summary>
        /// This method deserializes a byte array to a specified type.
        /// It uses a cache to store the parsers (included in the protobuf
        /// types) that have already been used. 
        /// </summary>
        /// <param name="bytes">The serialized data</param>
        /// <param name="type">The type of the Protobuf generated type that
        /// you want to deserialize to.</param>
        /// <returns></returns>
        public object Deserialize(byte[] bytes, Type type)
        {
            if (TypeCache.TryGetValue(type.FullName, out var parser))
            {
                return parser.ParseFrom(bytes);
            }

            parser = RegisterProtobufTypeParser(type);

            return parser?.ParseFrom(bytes);
        }

        public MessageParser RegisterProtobufTypeParser(Type messageType)
        {
            IMessage msg = Activator.CreateInstance(messageType) as IMessage;

            if (msg?.Descriptor == null)
                return null;

            var parser = msg.Descriptor.Parser;
            TypeCache.TryAdd(messageType.FullName, parser);

            return parser;
        }
    }
}