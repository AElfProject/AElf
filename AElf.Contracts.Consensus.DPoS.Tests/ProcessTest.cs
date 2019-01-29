using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    public sealed class ProcessTest : DPoSContractTestBase
    {
        private readonly ContractsShim _contracts;

        public ProcessTest()
        {
            _contracts = new ContractsShim(GetRequiredService<MockSetup>(), GetRequiredService<IExecutingService>());
        }

        [Fact]
        public void Initial_Consensus_WaitingTime()
        {
            // Arrange
            var stubMiner = new KeyPairGenerator().Generate();

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetCountingMilliseconds", stubMiner,
                DateTime.UtcNow.ToTimestamp());
            var actual = _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();

            // Assert
            Assert.Equal(10_000, actual);
        }

        [Fact]
        public void Initial_Consensus_GetInformation()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(new KeyPairGenerator().Generate());
            }

            var stubInitialInformation = new DPoSExtraInformation
            {
                InitialMiners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetNewConsensusInformation", stubMiners[0],
                stubInitialInformation.ToByteArray());
            var initialInformation =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<DPoSInformation>();

            // Assert
            Assert.NotNull(initialInformation);
            Assert.True(1 == initialInformation.NewTerm.FirstRound.RoundNumber);
            Assert.True(2 == initialInformation.NewTerm.SecondRound.RoundNumber);
        }

        [Fact]
        public void Initial_Consensus_Validate()
        {
            // Arrange information for initialization.
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(new KeyPairGenerator().Generate());
            }

            var stubInitialInformation = new DPoSInformation
            {
                WillUpdateConsensus = true,
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToMiners().GenerateNewTerm(4000),
                Sender = Address.FromPublicKey(stubMiners[0].PublicKey)
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "ValidateConsensus", stubMiners[1],
                stubInitialInformation.ToByteArray());
            var validationResult =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<ValidationResult>();

            // Assert
            Assert.True(validationResult?.Success);
        }

        [Fact]
        public void Initial_Consensus_GenerateTransaction()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(new KeyPairGenerator().Generate());
            }

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Index = 1}, stubInitialExtraInformation.ToByteArray());
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();

            // Assert
            Assert.NotNull(initialTransactions);
            Assert.True(initialTransactions.Transactions[0].MethodName == "InitialTerm");
        }
    }
}