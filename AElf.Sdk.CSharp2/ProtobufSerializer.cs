using System;
using System.Collections.Concurrent;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
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
        public T Deserialize<T>(byte[] bytes) where T : IMessage
        {
            if (!TypeCache.TryGetValue(typeof(T).FullName, out var parser))
            {
                parser = RegisterProtobufTypeParser<T>();
            }
            return (T)parser.ParseFrom(bytes);
        }

        private MessageParser RegisterProtobufTypeParser<T>() where T : IMessage
        {
            IMessage msg = (IMessage)Activator.CreateInstance(typeof(T));
            return msg.Descriptor.Parser;
        }
    }
}