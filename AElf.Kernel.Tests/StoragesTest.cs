using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Common;
using AElf.TestBase;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Tests
{
    public sealed class StoragesTest : AElfKernelIntegratedTest
    {
        private readonly IDataStore _dataStore;
        private readonly BlockTest _blockTest;
        private readonly IChainService _chainService;
        public ILogger<StoragesTest> Logger {get;set;}

        public StoragesTest()
        {
            _dataStore = GetRequiredService<IDataStore>();
            _blockTest = GetRequiredService<BlockTest>();
            _chainService = GetRequiredService<IChainService>();
            Logger= GetRequiredService<ILogger<StoragesTest>>();
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