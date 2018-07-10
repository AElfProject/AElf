using System;
using Akka.Actor;
using Akka.Serialization;
using Google.Protobuf;
using NServiceKit.Net30.Collections.Concurrent;

namespace AElf.Kernel.Concurrency.Execution.Messages
{
    public class AkkaProtobufSerializer: Serializer
    {
        private static readonly ConcurrentDictionary<string, MessageParser> TypeCache
            = new ConcurrentDictionary<string, MessageParser>();
        
        public AkkaProtobufSerializer(ExtendedActorSystem system) : base(system)
        {
        }
        
        public override int Identifier { get; } = 1234567;


        public override byte[] ToBinary(object obj)
        {
            IMessage message = obj as IMessage;
            return message?.ToByteArray();
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (TypeCache.TryGetValue(type.FullName, out var parser))
            {
                return parser.ParseFrom(bytes);
            }

            parser = RegisterProtobufTypeParser(type);

            return parser?.ParseFrom(bytes);
        }

        public override bool IncludeManifest { get; } = false;
        
        private MessageParser RegisterProtobufTypeParser(Type messageType)
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