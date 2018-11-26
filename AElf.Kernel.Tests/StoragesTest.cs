using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IDataStore _dataStore;
        private readonly BlockTest _blockTest;
        private readonly IChainService _chainService;
        private readonly ILogger _logger;

        public StoragesTest(IDataStore dataStore, BlockTest blockTest, IChainService chainService, ILogger logger)
        {
            _dataStore = dataStore;
            _blockTest = blockTest;
            _chainService = chainService;
            _logger = logger;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(new Transaction());
            block.AddTransaction(new Transaction());
            block.AddTransaction(new Transaction());
            block.AddTransaction(new Transaction());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Index = index;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.MerkleTreeRootOfWorldState = Hash.Generate();
            
            return block;
        }
    }
}