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

        [Fact]
        public async Task Initial_Command()
        {
            // Arrange
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));
            var stubMiner = CryptoHelpers.GenerateKeyPair();

            // Act
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GetConsensusCommand, stubMiner,
                DateTime.UtcNow.ToTimestamp(), stubMiner.PublicKey.ToHex());
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(DPoSContractConsts.AElfWaitFirstRoundTime, actual.CountingMilliseconds);
            Assert.Equal(int.MaxValue, actual.TimeoutMilliseconds);
            Assert.Equal(DPoSBehaviour.InitialTerm, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
        }

        [Fact]
        public async Task Initial_GetNewConsensusInformation()
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
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GetNewConsensusInformation,
                stubMiners[0], stubInitialInformation.ToByteArray());
            var information = DPoSInformation.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(1 == information.NewTerm.FirstRound.RoundNumber);
            Assert.True(2 == information.NewTerm.SecondRound.RoundNumber);
        }
        
        
        [Fact]
        public async Task Initial_Consensus_GenerateTransaction()
        {
            // Arrange
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = stubMiners[0].PublicKey.ToHex()
            };

            // Act
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GenerateConsensusTransactions,
                stubMiners[0], tester.Chain.LongestChainHeight, tester.Chain.BestChainHash.Value.Take(4).ToArray(),
                stubInitialExtraInformation.ToByteArray());
            var initialTransactions = TransactionList.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(initialTransactions.Transactions.First().MethodName == "InitialTerm");
            Assert.True(initialTransactions.Transactions.First().From ==
                        Address.FromPublicKey(stubMiners[0].PublicKey));
        }

        [Fact]
        public async Task NormalBlock_Consensus_WaitingTime()
        {
            // Arrange
            var tester1 = new ContractTester(ChainId);
            var addresses = await tester1.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var tester2 = new ContractTester(ChainId);
            await tester2.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var miner1 = stubMiners[0];
            var miner2 = stubMiners[1];

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = miner1.PublicKey.ToHex()
            };

            var txsBytes = await tester1.CallContractMethodAsync(addresses[1],
                ConsensusConsts.GenerateConsensusTransactions, miner1, tester1.Chain.LongestChainHeight,
                tester1.Chain.BestChainHash.Value.Take(4).ToArray(), stubInitialExtraInformation.ToByteArray());
            var txList = TransactionList.Parser.ParseFrom(txsBytes);
            var txs = txList.Transactions.ToList();

            tester1.SignTransaction(ref txs, miner1);

            var block = await tester1.MineABlockAsync(new List<Transaction>(), txs);

            await tester2.AddABlockAsync(block, new List<Transaction>(), txs);

            // Act
            var bytes = await tester2.CallContractMethodAsync(addresses[1], ConsensusConsts.GetConsensusCommand, miner2,
                DateTime.UtcNow.ToTimestamp(), miner2.PublicKey.ToHex());
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(actual.CountingMilliseconds != DPoSContractConsts.AElfWaitFirstRoundTime);
        }

        [Fact]
        public async Task NormalBlock_Consensus_GetInformation()
        {
            // Arrange
            var tester1 = new ContractTester(ChainId);
            var addresses = await tester1.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));

            var tester2 = new ContractTester(ChainId);
            await tester2.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));
            
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < 17; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }
            
            var miner1 = stubMiners[0];
            var miner2 = stubMiners[1];

            var stubInitialExtraInformation = new DPoSExtraInformation
            {
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateNewTerm(4000),
                MiningInterval = 4000,
                PublicKey = miner1.PublicKey.ToHex()
            };
/*

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
                .OutValue);*/
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
/*

        

        [Fact]
        public async Task NormalBlock_Consensus_Validate_Success()
        {
            // Arrange
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract));
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
        }*/
    }
}