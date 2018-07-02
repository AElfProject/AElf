﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Merkle;
using AElf.Kernel.Services;
using Akka.IO;
using Google.Protobuf;
using Moq;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class BlockGeneration
    {
        private readonly IBlockManager _blockManager;
        private readonly IWorldStateDictator _worldStateDictator;

        public BlockGeneration(IBlockManager blockManager, IWorldStateDictator worldStateDictator)
        {
            _blockManager = blockManager;
            _worldStateDictator = worldStateDictator;
        }


        public async Task SetWorldState()
        {
            var address = Hash.Generate();
            var accountDataProvider = await _worldStateDictator.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var key = new Hash("testkey".CalculateHash());
            var subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(key, data1);
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(key, data2);
            var data3= Hash.Generate().Value.ToArray();
            var subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(key, data3);
            var data4 = Hash.Generate().Value.ToArray();
            var subDataProvider4 = dataProvider.GetDataProvider("test4");
            await subDataProvider4.SetAsync(key, data4);
        }
        
        

        public Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync(Hash lastBlockHash, Hash txId1, Hash txId2, Hash h1, Hash h2)
        {
            var dict = new Dictionary<Hash, Change>
            {
                {
                    h1, new Change
                    {
                        After = Hash.Generate(),
                        Befores = {Hash.Generate(), Hash.Generate()},
                        TransactionIds = txId1,
                        LatestChangedBlockHash = lastBlockHash
                    }
                },
                {
                    h2, new Change
                    {
                        After = Hash.Generate(),
                        Befores = {Hash.Generate(), Hash.Generate()},
                        TransactionIds = txId2,
                        LatestChangedBlockHash = lastBlockHash
                    }
                }
            };
            return Task.FromResult(dict);
        }

        public async Task<Mock<IWorldStateDictator>> GetWorldStateManager(Hash lastBlockHash, Hash txId1, Hash txId2, Hash h1, Hash h2, Hash chainId)
        {
            var dic = new Dictionary<string, byte[]>();
            var mock = new Mock<IWorldStateDictator>();
            mock.Setup(ws => ws.GetChangesDictionaryAsync()).Returns(() => GetChangesDictionaryAsync(lastBlockHash, txId1, txId2, h1, h2));

            var changes = await GetChangesDictionaryAsync(lastBlockHash, txId1, txId2, h1, h2);
            var changeDict = new ChangesDict
            {
                Dict =
                {
                    new PairHashChange
                    {
                        Key = h1,
                        Value = changes[h1]
                    },
                    new PairHashChange
                    {
                        Key = h2,
                        Value = changes[h2]
                    }
                }
            };
            
            mock.Setup(ws => ws.SetWorldStateAsync(It.IsAny<Hash>())).Returns( () =>
            {
                Hash wsKey = chainId.CalculateHashWith(lastBlockHash);
                dic[wsKey.Value.ToByteArray().ToHex()] = changeDict.ToByteArray();

                //Refresh _preBlockHash after setting WorldState.
                //_preBlockHash = preBlockHash;
                return Task.CompletedTask;
            });
            
            //IWorldState w = new WorldState(changeDict);
            
            var wsMock = new Mock<IWorldState>();
            wsMock.Setup(w => w.GetWorldStateMerkleTreeRootAsync()).Returns(async () =>
            {
                var merkleTree = new BinaryMerkleTree();
                foreach (var pair in changeDict.Dict)
                {
                    merkleTree.AddNode(pair.Key);
                }

                return await Task.FromResult(merkleTree.ComputeRootHash());
            });
            
            mock.Setup(ws => ws.GetWorldStateAsync(It.IsAny<Hash>())).Returns(() => Task.FromResult(wsMock.Object));
            return mock;
        }

        public Mock<IChainManager> GetChainManager(Hash lastBlockHash)
        {
            var mock = new Mock<IChainManager>();
            mock.Setup(c => c.GetChainLastBlockHash(It.IsAny<Hash>())).Returns(Task.FromResult(lastBlockHash));
            return mock;
        }

        
    }
}