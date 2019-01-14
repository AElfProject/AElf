using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class BlockchainStateManagerTests : AElfKernelTestBase
    {
        private BlockchainStateManager _blockchainStateManager;

        private List<TestPair> _tv;
        
        public BlockchainStateManagerTests()
        {
            _blockchainStateManager = GetRequiredService<BlockchainStateManager>();
            _tv =new List<TestPair>();
            for (int i = 0; i < 200; i++)
            {
                _tv.Add(new TestPair()
                {
                    BlockHash = Hash.Generate(),
                    BlockHeight = i,
                    Key = $"key{i}",
                    Value = ByteString.CopyFromUtf8($"value{i}")
                });
            }
        }


        class TestPair
        {
            public Hash BlockHash;
            public long BlockHeight;
            public string Key;
            public ByteString Value;
        }
        
        [Fact]
        public async Task TestAddBlockStateSet()
        {
            // one level tests without best chain state
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
            {
                BlockHash = _tv[1].BlockHash,
                BlockHeight = _tv[1].BlockHeight,
                PreviousHash = null,
                Changes =
                {
                    {
                        _tv[1].Key,
                        new VersionedState()
                        {
                            Key = _tv[1].Key,
                            Value = _tv[1].Value
                        }
                    },
                    {
                        _tv[2].Key,
                        new VersionedState()
                        {
                            Key = _tv[2].Key,
                            Value = _tv[2].Value
                        }
                    },
                }
            });
            
            (await _blockchainStateManager.GetStateAsync(_tv[1].Key,_tv[1].BlockHeight,_tv[1].BlockHash))
                .Value.ShouldBe(_tv[1].Value);
            
            (await _blockchainStateManager.GetStateAsync(_tv[2].Key,_tv[1].BlockHeight,_tv[1].BlockHash))
                .Value.ShouldBe(_tv[2].Value);
            
            //two level tests without best chain state
        }
    }
}