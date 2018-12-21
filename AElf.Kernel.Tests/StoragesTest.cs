using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly BlockTest _blockTest;
        private readonly IChainService _chainService;
        private readonly ILogger _logger;

        public StoragesTest(BlockTest blockTest, IChainService chainService, ILogger logger)
        {
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