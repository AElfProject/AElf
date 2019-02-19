using System;
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
        private readonly ContractsShim _contracts;

        public ProcessTest()
        {
            _contracts = new ContractsShim(GetRequiredService<MockSetup>(), GetRequiredService<IExecutingService>());
        }
/*

        [Fact]
        public void Initial_Consensus_WaitingTime()
        {
            // Arrange
            var stubMiner = CryptoHelpers.GenerateKeyPair();

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
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
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

        [Fact]
        public void ExtraBlock_Consensus_WaitingTime()
        {
            // Arrange
            const int MinersCount = 3;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
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

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetCountingMilliseconds", stubMiners[0],
                initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp());
            var actual1 = _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetCountingMilliseconds", stubMiners[1],
                initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp());
            var actual2 = _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetCountingMilliseconds", stubMiners[2],
                initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp());
            var actual3 = _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();

            // Assert
            Assert.True(actual1 != int.MaxValue && actual1 > 0);
            Assert.True(actual2 != int.MaxValue && actual2 > 0);
            Assert.True(actual3 != int.MaxValue && actual3 > 0);
        }

        [Fact]
        public void ExtraBlock_NextRound_Consensus_Validate_Success()
        {
            // Arrange
            const int MinersCount = 3;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
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

            var stubNextRoundInformation = new DPoSInformation
            {
                WillUpdateConsensus = true,
                Forwarding = new Forwarding
                {
                    CurrentAge = 1,
                    CurrentRound = initialTerm.FirstRound.SupplementForFirstRound(),
                    NextRound = initialTerm.SecondRound
                }
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "ValidateConsensus", stubMiners[1],
                stubNextRoundInformation.ToByteArray());
            var validationResult =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<ValidationResult>();

            // Assert
            Assert.True(validationResult?.Success);
        }

        [Fact]
        public void ExtraBlock_NextRound_Consensus_GetInformation()
        {
            // Arrange
            const int MinersCount = 3;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
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

            var stubExtraInformation = new DPoSExtraInformation
            {
                Timestamp = initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp()
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetNewConsensusInformation", stubMiners[0],
                stubExtraInformation.ToByteArray());
            var nextRoundInformation =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<DPoSInformation>();

            // Assert
            Assert.NotNull(nextRoundInformation);
            Assert.True(nextRoundInformation.WillUpdateConsensus);
            Assert.NotNull(nextRoundInformation.Forwarding);
        }

        [Fact]
        public void ExtraBlock_NextTerm_Consensus_GetInformation_EmptyNewTerm()
        {
            // Arrange
            const int MinersCount = 3;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
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

            var stubExtraInformation = new DPoSExtraInformation
            {
                Timestamp = initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp(),
                ChangeTerm = true
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GetNewConsensusInformation", stubMiners[0],
                stubExtraInformation.ToByteArray());
            var nextRoundInformation =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<DPoSInformation>();

            // Assert
            Assert.NotNull(nextRoundInformation);
            Assert.True(nextRoundInformation.WillUpdateConsensus);
            // NewTerm is empty because no victory.
            Assert.NotNull(nextRoundInformation.NewTerm);
        }

        [Fact]
        public void ExtraBlock_NextRound_Consensus_GenerateTransactions()
        {
            // Arrange
            const int MinersCount = 3;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
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

            var stubExtraInformation = new DPoSExtraInformation
            {
                Timestamp = initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp(),
                Forwarding = new Forwarding
                {
                    CurrentAge = 1,
                    CurrentRound = initialTerm.FirstRound.SupplementForFirstRound(),
                    NextRound = initialTerm.SecondRound
                }
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 2}, stubExtraInformation.ToByteArray());
            var nextRoundTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(nextRoundTransactions);
            var nextRoundTransaction = nextRoundTransactions.Transactions.First();

            // Assert
            Assert.True(nextRoundTransaction.MethodName == "NextRound");
        }

        [Fact]
        public void ExtraBlock_NextTerm_Consensus_GenerateTransactions()
        {
            // Arrange
            const int MinersCount = 3;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
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

            var stubExtraInformation = new DPoSExtraInformation
            {
                Timestamp = initialTerm.Timestamp.ToDateTime().AddMilliseconds(MinersCount * 4000 + 2000).ToTimestamp(),
                ChangeTerm = true,
                NewTerm = new Term()
            };

            // Act
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, "GenerateConsensusTransactions",
                stubMiners[0], new BlockHeader {Height = 2}, stubExtraInformation.ToByteArray());
            var nextRoundTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();

            // Assert
            Assert.NotNull(nextRoundTransactions);
            Assert.True(nextRoundTransactions.Transactions.Count == 4);
        }*/
    }
}