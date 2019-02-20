//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.SmartContract;
//using AElf.ChainController;
//using Google.Protobuf.WellKnownTypes;
//using Xunit;
//using AElf.Common;
//using AElf.Kernel;
//using AElf.TestBase;
//using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Tests
{
    /*
    public sealed class StoragesTest : AElfKernelTestBase
    {
        private readonly BlockTest _blockTest;
        private readonly IChainService _chainService;
        public ILogger<StoragesTest> Logger {get;set;}

        public StoragesTest()
        {
            _blockTest = GetRequiredService<BlockTest>();
            _chainService = GetRequiredService<IChainService>();
            Logger= GetRequiredService<ILogger<StoragesTest>>();
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Block CreateBlock(Hash preBlockHash, int chainId, ulong height)
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
            block.Header.Height = height;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.MerkleTreeRootOfWorldState = Hash.Generate();
            
            return block;
        }
    }
    */
}