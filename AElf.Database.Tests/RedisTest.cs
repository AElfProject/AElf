using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Xunit;

namespace AElf.Database.Tests
{
    public class RedisTest
    {
        [Fact]
        public async Task RedisHelperTest()
        {
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

            var serializedValue = change.ToByteArray();

            var success = await RedisHelper.SetAsync(key, serializedValue);
            Assert.True(success);
            
            var getChange = await RedisHelper.GetAsync(key);
            var getDeserializedChange = Change.Parser.ParseFrom(getChange);
            
            Assert.True(change.After == getDeserializedChange.After);
            Assert.True(change.GetLastHashBefore() == getDeserializedChange.GetLastHashBefore());
            Assert.True(change.LatestChangedBlockHash == getDeserializedChange.LatestChangedBlockHash);
            Assert.True(change.TransactionIds == getDeserializedChange.TransactionIds);
        }
    }
}