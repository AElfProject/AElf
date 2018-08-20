using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.BlockSyncTests;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateTest
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly BlockTest _blockTest;
        private readonly ITxPoolService _txPoolService;
        private readonly IChainService _chainService;
        private readonly IChainManagerBasic _chainManager;
        private readonly IBlockManagerBasic _blockManger;

        public WorldStateTest(IDataStore dataStore, BlockTest blockTest, ILogger logger,
            ITxPoolService txPoolService, IChainService chainService, IChainManagerBasic chainManager, IBlockManagerBasic blockManager)
        {
            _dataStore = dataStore;
            _blockTest = blockTest;
            _logger = logger;
            _txPoolService = txPoolService;
            _chainService = chainService;
            _chainManager = chainManager;
            _blockManger = blockManager;
        }

        /// <summary>
        /// the hash of block created by this method will be changed when appending to a chain.
        /// (basically change the block header's Index value)
        /// </summary>
        /// <param name="preBlockHash"></param>
        /// <param name="chainId"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Index = index;
            block.Header.MerkleTreeRootOfWorldState = Hash.Generate();
            block.Body.BlockHeader = block.Header.GetHash();

            System.Diagnostics.Debug.WriteLine($"Hash of height {index}: {block.GetHash().Value.ToByteArray().ToHex()}\twith previous hash {preBlockHash.Value.ToByteArray().ToHex()}");

            return block;
        }
    }
}