using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.Extensions;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Xunit;

namespace AElf.Contracts.Consensus.Tests
{
    public class ProcessTest
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        [Fact]
        public async Task InitialTermTest()
        {
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(DPoS.ConsensusContract));
            
            var miners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                miners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var initialTerm =
                new Miners {PublicKeys = {miners.Select(m => m.PublicKey.ToHex())}}.GenerateNewTerm(4000);
            var bytes = await tester.CallContractMethodAsync(addresses[1], "InitialTerm", miners[0],
                initialTerm);

            var result = ActionResult.Parser.ParseFrom(bytes);
            
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DeployContractTest()
        {
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero));

            var tx = tester.GenerateTransaction(addresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await tester.MineABlockAsync(new List<Transaction> {tx});

            var chain = await tester.GetChainAsync();

            Assert.Equal(2UL, chain.BestChainHeight);
            
            var tokenContractAddress = Address.BuildContractAddress(ChainHelpers.ConvertBase58ToChainId("AELF"), 1);

            await tester.CallContractMethodAsync(tokenContractAddress, "Initialize",
                CryptoHelpers.GenerateKeyPair(), "ELF", "AElf Token", 1000000000, 2);
        }
    }
}