using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    public class ConsensusProcessTest
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        [Fact]
        public async Task Initial_Command()
        {
            // Arrange
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));
            var stubMiner = CryptoHelpers.GenerateKeyPair();

            // Act
            var bytes = await tester.ExecuteContractAsync(addresses[1], ConsensusConsts.GetConsensusCommand, stubMiner,
                DateTime.UtcNow.ToTimestamp(), stubMiner.PublicKey.ToHex());
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(DPoSContractConsts.AElfWaitFirstRoundTime, actual.CountingMilliseconds);
            Assert.Equal(int.MaxValue, actual.TimeoutMilliseconds);
            Assert.Equal(DPoSBehaviour.InitialTerm, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
        }


        [Fact]
        public async Task Initial_GenerateConsensusTransactions()
        {
            // Arrange
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialInformation = new DPoSExtraInformation
            {
                InitialMiners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000,
                PublicKey = stubMiners[0].PublicKey.ToHex()
            };

            // Act
            var bytes = await tester.ExecuteContractAsync(addresses[1], ConsensusConsts.GenerateConsensusTransactions,
                stubMiners[0], stubInitialInformation);
            var information = DPoSInformation.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(1 == information.NewTerm.FirstRound.RoundNumber);
            Assert.True(2 == information.NewTerm.SecondRound.RoundNumber);
        }
    }
}