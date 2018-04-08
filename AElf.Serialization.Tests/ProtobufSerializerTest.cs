using System.Linq;
using AElf.Serialization.Protobuf;
using AElf.Serialization.Protobuf.Generated;
using Google.Protobuf;
using Xunit;

namespace AElf.Serialization.Tests
{
    public class ProtobufSerializerTest
    {
        [Fact]
        public void Deserialize_NonMessageType_ReturnsNull()
        {
            ProtobufSerializer serializer = new ProtobufSerializer();
            
            byte[] someBytes = {0x20, 0x21, 0x22};
            
            // Try and deserialize the byte array to a type that does not inherite from IMessage
            NonIMessageClass bigInt = serializer.Deserialize<NonIMessageClass>(someBytes);
            
            Assert.Null(bigInt);
        }
        
        [Fact]
        public void Serialize_NullObject_ReturnsNull()
        {
            ProtobufSerializer serializer = new ProtobufSerializer();
            
            byte[] bigInt = serializer.Serialize(null);
            
            Assert.Null(bigInt);
        }

        [Fact]
        public void SerializeAndDeserializeAccount_Test()
        {
            ProtobufSerializer serializer = new ProtobufSerializer();

            byte[] accountAdress = {0x20, 0x21, 0x22};
            
            ProtoAccount protoAccount = new ProtoAccount();
            protoAccount.Address = ByteString.CopyFrom(accountAdress);

            // Serialize
            byte[] serializedAccount = serializer.Serialize(protoAccount);

            // Deserialize
            ProtoAccount deserializedAccount = serializer.Deserialize<ProtoAccount>(serializedAccount);
            
            Assert.NotNull(deserializedAccount);
            
            bool seqEq = accountAdress.SequenceEqual(deserializedAccount.Address);
            
            Assert.True(seqEq);
        }
    }

    public class NonIMessageClass
    {
    }
}