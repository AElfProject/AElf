using System;
using System.Collections.Generic;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.MultiToken;
using AElf.OS.Node.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.Node
{
    public class IOsBlockchainNodeContextServiceTests: OSCoreTestBase
    {
        [Fact]
        public void Add_GenesisSmartContract_Test()
        {
            var genesisContracts = new List<GenesisSmartContractDto>();
            genesisContracts.AddGenesisSmartContracts(typeof(ConsensusContract), typeof(TokenContract));
            genesisContracts.Count.ShouldBe(2);
        }
    }
}