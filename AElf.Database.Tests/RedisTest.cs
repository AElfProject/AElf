using System;
using System.Linq;
using AElf.Kernel;
using ServiceStack;
using ServiceStack.Redis;
using Xunit;
using ProtobufSerializer = AElf.Serialization.Protobuf.ProtobufSerializer;

namespace AElf.Database.Tests
{
    public class RedisTest
    {
        private const string IpAddress = "127.0.0.1";

        private const int Port = 6379;

        private static RedisClient RedisClient => new RedisClient(IpAddress, Port);

        [Fact]
        public void BasicTest()
        {
            var serializer = new ProtobufSerializer();
            
            const string key = "OneChange";

            var change = new Change
            {
                After = Hash.Generate(),
                LatestChangedBlockHash = Hash.Generate(),
                TransactionIds = Hash.Generate()
            };
            change.AddHashBefore(Hash.Generate());
            change.AddHashBefore(Hash.Generate());
            change.AddHashBefore(Hash.Generate());
            change.AddHashBefore(Hash.Generate());

            var serializedValue = serializer.Serialize(change);

            var success = RedisClient.Set(key, serializedValue);
            Assert.True(success);
            
            var getChange = RedisClient.Get(key);
            var getDeserializedChange = serializer.Deserialize<Change>(getChange);
            
            Assert.True(change.After == getDeserializedChange.After);
            Assert.True(change.GetLastHashBefore() == getDeserializedChange.GetLastHashBefore());
        }
    }
}