/*using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    // ReSharper disable InconsistentNaming
    public sealed class ProcessTest : DPoSContractTestBase
    {

        [Fact]
        public void Initial_Consensus_Validate_Success()
        {
            // Arrange information for initialization.
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialInformation = new DPoSInformation
            {
                WillUpdateConsensus = true,
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToMiners().GenerateNewTerm(4000),
                Sender = Address.FromPublicKey(stubMiners[0].PublicKey),
                MinersList = {stubMiners.Select(m => Address.FromPublicKey(m.PublicKey)).ToList()}
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
        public void Initial_Consensus_Validate_Failed_SenderIsNotAMiner()
        {
            // Arrange information for initialization.
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialInformation = new DPoSInformation
            {
                WillUpdateConsensus = true,
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToMiners().GenerateNewTerm(4000),
                Sender = Address.FromPublicKey(CryptoHelpers.GenerateKeyPair().PublicKey),
                MinersList = {stubMiners.Select(m => Address.FromPublicKey(m.PublicKey)).ToList()}
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "ValidateConsensus", stubMiners[1],
                stubInitialInformation.ToByteArray());
            var validationResult =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<ValidationResult>();

            // Assert
            Assert.False(validationResult?.Success);
        }

        [Fact]
        public void Initial_Consensus_GenerateTransaction()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 1}, stubInitialExtraInformation.ToByteArray());
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();

            // Assert
            Assert.NotNull(initialTransactions);
            Assert.True(initialTransactions.Transactions.First().MethodName == "InitialTerm");
            Assert.True(initialTransactions.Transactions.First().From ==
                        Address.FromPublicKey(stubMiners[0].PublicKey));
        }

        [Fact]
        public void NormalBlock_Consensus_WaitingTime()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000
            };

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 1}, stubInitialExtraInformation.ToByteArray());
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            _contracts.ExecuteTransaction(initialTransactions.Transactions.First(), stubMiners[0]);

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetCountingMilliseconds", stubMiners[0],
                DateTime.UtcNow.ToTimestamp());
            var actual = _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();

            // Assert
            // This fake node should produce block immediately.
            Assert.True(actual != 10000 && actual > 0);
        }

        [Fact]
        public void NormalBlock_Consensus_GetInformation()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000
            };

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 1}, stubInitialExtraInformation.ToByteArray());
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            _contracts.ExecuteTransaction(initialTransactions.Transactions.First(), stubMiners[0]);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation = new DPoSExtraInformation
            {
                HashValue = outValue,
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetNewConsensusInformation",
                stubMiners[0], stubExtraInformation.ToByteArray());
            var newConsensusInformation =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<DPoSInformation>();

            // Assert
            Assert.NotNull(newConsensusInformation);
            // The out value of this fake node should be filled.
            Assert.NotNull(newConsensusInformation.CurrentRound.RealTimeMinersInfo[stubMiners[0].PublicKey.ToHex()]
                .OutValue);
        }

        [Fact]
        public void NormalBlock_Consensus_Validate_Success()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000
            };

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 1}, stubInitialExtraInformation.ToByteArray());
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            _contracts.ExecuteTransaction(initialTransactions.Transactions.First(), stubMiners[0]);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation = new DPoSExtraInformation
            {
                HashValue = outValue,
            };

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetNewConsensusInformation",
                stubMiners[0], stubExtraInformation.ToByteArray());
            var stubInformation =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<DPoSInformation>();

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "ValidateConsensus", stubMiners[1],
                stubInformation.ToByteArray());
            var validationResult =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<ValidationResult>();

            // Assert
            Assert.True(validationResult?.Success);
        }

        [Fact]
        public void NormalBlock_Consensus_GenerateTransaction()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var initialTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000);
            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = initialTerm,
                MiningInterval = 4000
            };

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 1}, stubInitialExtraInformation.ToByteArray());
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            // InitialTerm
            _contracts.ExecuteTransaction(initialTransactions.Transactions.First(), stubMiners[0]);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation = new DPoSExtraInformation
            {
                ToPackage = new ToPackage
                {
                    OutValue = outValue,
                    RoundId = initialTerm.FirstRound.RoundId,
                }
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 2}, stubExtraInformation.ToByteArray());
            var normalTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            var normalTransaction = normalTransactions?.Transactions.First();

            // Assert
            Assert.NotNull(normalTransactions);
            Assert.Single(normalTransactions.Transactions);
            Assert.True(normalTransaction.MethodName == "PackageOutValue");
        }

        ///*

    }
}*/