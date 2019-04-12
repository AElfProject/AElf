using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus
{
    public class ConsensusTransactionGeneratorTests : ConsensusTestBase
    {
        private ISystemTransactionGenerator _systemTransactionGenerator;

        public ConsensusTransactionGeneratorTests()
        {
            _systemTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
        }

        [Fact]
        public void GenerateTransactions()
        {
            var address = Address.Generate();
            var blockHeight = 100;
            var hash = Hash.Generate();
            var transactions = new List<Transaction>();

            _systemTransactionGenerator.GenerateTransactions(address, blockHeight, hash, ref transactions);
            transactions.Count.ShouldBe(3);
            transactions.Select(t=>t.MethodName).ShouldAllBe(x => x == ConsensusConsts.GenerateConsensusTransactions);
        }
    }
}