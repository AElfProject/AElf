using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.Extensions;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Type = System.Type;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    public class ConsensusProcessTest
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        private int _miningInterval = 4000;

        [Fact]
        public async Task Initial_Command()
        {
            // Arrange
            var stubMiner = CryptoHelpers.GenerateKeyPair();
            var tester = new ContractTester(ChainId, stubMiner);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            // Act
            var firstExtraInformation = new DPoSExtraInformation
            {
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                PublicKey = stubMiner.PublicKey.ToHex(),
                IsBootMiner = true
            };
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GetConsensusCommand,
                firstExtraInformation.ToByteArray());
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(_miningInterval, actual.CountingMilliseconds);
            Assert.Equal(int.MaxValue, actual.TimeoutMilliseconds);
            Assert.Equal(DPoSBehaviour.InitialTerm, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
        }

        [Fact]
        public async Task Initial_Command_ExtensionMethod()
        {
            // Arrange
            var stubMiner = CryptoHelpers.GenerateKeyPair();
            var tester = new ContractTester(ChainId, stubMiner);
            await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            // Act
            var actual = await tester.GetConsensusCommand();

            // Assert
            Assert.Equal(_miningInterval, actual.CountingMilliseconds);
            Assert.Equal(int.MaxValue, actual.TimeoutMilliseconds);
            Assert.Equal(DPoSBehaviour.InitialTerm, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
        }
        
        [Fact]
        public async Task Initial_GetNewConsensusInformation()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialInformation = new DPoSExtraInformation
            {
                InitialMiners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000,
                PublicKey = stubMiners[0].PublicKey.ToHex()
            };

            // Act
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GetNewConsensusInformation,
                stubInitialInformation.ToByteArray());
            var information = DPoSInformation.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(1 == information.NewTerm.FirstRound.RoundNumber);
            Assert.True(2 == information.NewTerm.SecondRound.RoundNumber);
        }
        
        [Fact]
        public async Task Initial_GetNewConsensusInformation_ExtensionMethod()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
            var tester = new ContractTester(ChainId, stubMiners[0]);
            await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialInformation = new DPoSExtraInformation
            {
                InitialMiners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000,
                PublicKey = stubMiners[0].PublicKey.ToHex()
            };

            // Act
            var information = await tester.GetNewConsensusInformation(stubInitialInformation);

            // Assert
            Assert.True(1 == information.NewTerm.FirstRound.RoundNumber);
            Assert.True(2 == information.NewTerm.SecondRound.RoundNumber);
        }
        
        [Fact]
        public async Task Initial_GenerateConsensusTransactions()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var miner = stubMiners[0];
            var tester = new ContractTester(ChainId, miner);
            var addresses = await tester.InitialChainAsync( typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = miner.PublicKey.ToHex()
            };

            // Act
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GenerateConsensusTransactions,
                 tester.Chain.LongestChainHeight, tester.Chain.BestChainHash.Value.Take(4).ToArray(),
                stubInitialExtraInformation.ToByteArray());
            var initialTransactions = TransactionList.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(initialTransactions.Transactions.First().MethodName == "InitialTerm");
            Assert.True(initialTransactions.Transactions.First().From == Address.FromPublicKey(miner.PublicKey));
        }
        
        [Fact]
        public async Task Initial_GenerateConsensusTransactions_ExtensionMethod()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
            var tester = new ContractTester(ChainId, stubMiners[0]);
            await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = stubMiners[0].PublicKey.ToHex()
            };

            // Act
            var initialTransactions = await tester.GenerateConsensusTransactions(stubInitialExtraInformation);

            // Assert
            Assert.True(initialTransactions[0].MethodName == "InitialTerm");
            Assert.True(initialTransactions[0].From == Address.FromPublicKey(stubMiners[0].PublicKey));
        }

        [Fact]
        public async Task NormalBlock_GetConsensusCommand()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var miner1 = stubMiners[0];
            var miner2 = stubMiners[1];
            
            var tester1 = new ContractTester(ChainId, miner1);
            await tester1.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var tester2 = new ContractTester(ChainId, miner2);
            await tester2.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = miner1.PublicKey.ToHex()
            };

            await tester1.GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, tester2);

            // Act
            var actual = await tester2.GetConsensusCommand();

            // Assert
            Assert.True(actual.CountingMilliseconds != _miningInterval);
        }

        [Fact]
        public async Task NormalBlock_Consensus_GetInformation()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
            
            var miner1 = stubMiners[0];
            var miner2 = stubMiners[1];
            
            var tester1 = new ContractTester(ChainId, miner1);
            await tester1.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var tester2 = new ContractTester(ChainId, miner2);
            await tester2.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = miner1.PublicKey.ToHex()
            };

            await tester1.GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, tester2);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation = new DPoSExtraInformation
            {
                OutValue = outValue,
                PublicKey = miner2.PublicKey.ToHex()
            };

            // Act
            var newConsensusInformation = await tester2.GetNewConsensusInformation(stubExtraInformation);
            
            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(outValue, newConsensusInformation.CurrentRound.RealTimeMinersInfo[miner2.PublicKey.ToHex()]
                .OutValue);
        }
        
        private async Task<List<ContractTester>> CreateTesters(int number, params Type[] contractTypes)
        {
            var testers = new List<ContractTester>();
            for (var i = 0; i < number; i++)
            {
                var tester = new ContractTester(ChainId);
                await tester.InitialChainAsync(contractTypes);
                testers.Add(tester);
            }

            return testers;
        }

        [Fact]
        public async Task NormalBlock_ValidationConsensus_Success()
        {
            // Arrange
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var miner1 = stubMiners[0];
            var miner2 = stubMiners[1];

            var tester1 = new ContractTester(ChainId, miner1);
            await tester1.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var tester2 = new ContractTester(ChainId, miner2);
            await tester2.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = miner1.PublicKey.ToHex(),
                InitialMiners = {stubMiners.Select(p => p.PublicKey.ToHex())}
            };

            await tester1.GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, tester2);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation = new DPoSExtraInformation
            {
                OutValue = outValue,
                PublicKey = miner2.PublicKey.ToHex(),
            };

            var newInformation = await tester2.GetNewConsensusInformation(stubExtraInformation);

            // Act
            var validationResult = await tester1.ValidateConsensus(newInformation);

            // Assert
            Assert.True(validationResult?.Success);
        }

/*

        
        [Fact]
        public async Task NormalBlock_Consensus_GenerateTransaction()
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