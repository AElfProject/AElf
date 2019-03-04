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
            var firstTriggerInformation = new DPoSTriggerInformation
            {
                PublicKey = stubMiner.PublicKey.ToHex(),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                IsBootMiner = true,
            };
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GetConsensusCommand,
                firstTriggerInformation.ToByteArray());
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(8000, actual.CountingMilliseconds);//For now the 8000 is hard coded in contract code.
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
            Assert.Equal(8000, actual.CountingMilliseconds);
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

            var triggerInformation = GetTriggerInformationForInitialTerm(stubMiners);

            // Act
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GetNewConsensusInformation,
                triggerInformation.ToByteArray());
            var information = DPoSInformation.Parser.ParseFrom(bytes);
            var round = information.Round;
            
            // Assert
            Assert.Equal(stubMiners[0].PublicKey.ToHex(), information.SenderPublicKey);
            Assert.Equal(DPoSBehaviour.InitialTerm, information.Behaviour);
            // Check the basic information of first round.
            Assert.True(1 == round.RoundNumber);
            Assert.Equal(17,
                round.RealTimeMinersInformation.Values.Count(m =>
                    m.Signature != null && m.ExpectedMiningTime != null && m.Order > 0 && m.PromisedTinyBlocks == 1));
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

            var stubInitialInformation = GetTriggerInformationForInitialTerm(stubMiners);

            // Act
            var information = await tester.GetNewConsensusInformation(stubInitialInformation);
            var round = information.Round;
            
            // Assert
            Assert.Equal(stubMiners[0].PublicKey.ToHex(), information.SenderPublicKey);
            Assert.Equal(DPoSBehaviour.InitialTerm, information.Behaviour);
            // Check the basic information of first round.
            Assert.True(1 == round.RoundNumber);
            Assert.Equal(17,
                round.RealTimeMinersInformation.Values.Count(m =>
                    m.Signature != null && m.ExpectedMiningTime != null && m.Order > 0 && m.PromisedTinyBlocks == 1));
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

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(stubMiners);

            // Act
            var bytes = await tester.CallContractMethodAsync(addresses[1], ConsensusConsts.GenerateConsensusTransactions,
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

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(stubMiners);

            // Act
            var initialTransactions = await tester.GenerateConsensusTransactions(stubInitialExtraInformation);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialTerm.ToString(), initialTransactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(stubMiners[0].PublicKey), initialTransactions.First().From);
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

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(stubMiners);

            await tester1.GenerateConsensusTransactionsAndMineABlock(triggerInformationForInitialTerm, tester2);

            // Act
            var actual = await tester2.GetConsensusCommand();

            // Assert
            Assert.Equal(DPoSBehaviour.PackageOutValue, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
            Assert.True(actual.CountingMilliseconds != _miningInterval);
        }

        [Fact]
        public async Task NormalBlock_GetNewConsensusInformation()
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

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(stubMiners);

            await tester1.GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, tester2);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(tester2.CallOwnerKeyPair.PublicKey.ToHex(), inValue);

            // Act
            var newConsensusInformation = await tester2.GetNewConsensusInformation(stubExtraInformation);
            
            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(outValue, newConsensusInformation.Round.RealTimeMinersInformation[miner2.PublicKey.ToHex()]
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

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(stubMiners);

            await tester1.GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, tester2);

            var inValue = Hash.Generate();
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(tester2.CallOwnerKeyPair.PublicKey.ToHex(), inValue);

            var newInformation = await tester2.GetNewConsensusInformation(stubExtraInformation);

            // Act
            var validationResult = await tester1.ValidateConsensus(newInformation);

            // Assert
            Assert.True(validationResult?.Success);
        }
        
        [Fact]
        public async Task NormalBlock_GenerateConsensusTransactions()
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

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(stubMiners);

            await tester1.GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, tester2);

            var inValue = Hash.Generate();
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(tester2.CallOwnerKeyPair.PublicKey.ToHex(), inValue);

            // Act
            var consensusTransactions = await tester2.GenerateConsensusTransactions(stubExtraInformation);
            
            // Assert
            Assert.NotNull(consensusTransactions);
            Assert.Equal(DPoSBehaviour.PackageOutValue.ToString(), consensusTransactions.First().MethodName);
        }

        private DPoSTriggerInformation GetTriggerInformationForInitialTerm(List<ECKeyPair> stubMiners)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = stubMiners[0].PublicKey.ToHex(),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Miners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000,
            };
        }

        private DPoSTriggerInformation GetTriggerInformationForNormalBlock(string publicKey, Hash currentInValue,
            Hash previousInValue = null)
        {
            if (previousInValue == null)
            {
                previousInValue = Hash.Default;
            }
            
            return new DPoSTriggerInformation
            {
                PublicKey = publicKey,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                PreviousInValue = previousInValue,
                CurrentInValue = currentInValue
            };
        }
        
        private DPoSTriggerInformation GetTriggerInformationForNextRoundOrTerm(string publicKey)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = publicKey,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
            };
        }
    }
}