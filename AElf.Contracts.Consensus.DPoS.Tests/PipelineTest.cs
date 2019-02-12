using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    // ReSharper disable InconsistentNaming
    public sealed class PipelineTest : DPoSContractTestBase
    {
        private readonly ContractsShim _contracts;

        public PipelineTest()
        {
            _contracts = new ContractsShim(GetRequiredService<MockSetup>(), GetRequiredService<IExecutingService>());
        }

        enum ConsensusMethod
        {
            GetCountingMilliseconds,
            GetNewConsensusInformation,
            GenerateConsensusTransactions,
            ValidateConsensus
        }

        [Fact]
        public void ConsensusInitializationTest()
        {
            const int MinersCount = 17;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var initialMiner = stubMiners[0];

            // When start the node, query next mining time. The mining time should be 10_000.

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GetCountingMilliseconds.ToString(),
                initialMiner, Timestamp.FromDateTime(DateTime.UtcNow));
            var countingMilliseconds = GetReturnData().DeserializeToInt32();
            Assert.Equal(10_000, countingMilliseconds);

            // Let's say 10s has passed, this node begin to acquire consensus information.

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GetNewConsensusInformation.ToString(),
                initialMiner, new DPoSExtraInformation
                {
                    InitialMiners = {stubMiners.Select(m => m.PublicKey.ToHex())}
                });
            var consensusInformation = GetReturnData().DeserializeToPbMessage<DPoSInformation>();

            Assert.NotNull(consensusInformation);
            Assert.True(consensusInformation.NewTerm.FirstRound.RoundNumber == 1);
            Assert.True(consensusInformation.NewTerm.SecondRound.RoundNumber == 2);

            // Then this node has to use consensus contract to generate a transaction to update State Database.

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GenerateConsensusTransactions.ToString(),
                initialMiner,
                new BlockHeader {Index = 1},
                new DPoSExtraInformation
                {
                    NewTerm = consensusInformation.NewTerm,

                });
            var initialTransactions = GetReturnData().DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            var initialTransaction = initialTransactions.Transactions.First();

            Assert.True(initialTransaction.MethodName == "InitialTerm");
        }

        [Fact]
        public void ConsensusSyncTest()
        {
            const int MinersCount = 17;
            var stubMiners = new List<ECKeyPair>();
            for (var i = 0; i < MinersCount; i++)
            {
                stubMiners.Add(CryptoHelpers.GenerateKeyPair());
            }

            var initialMiner = stubMiners[0];
            var miner = stubMiners[1];

            var initialInformation = new DPoSInformation
            {
                Sender = Address.FromPublicKey(stubMiners[0].PublicKey),
                WillUpdateConsensus = true,
                NewTerm = stubMiners.Select(m => m.PublicKey.ToHex()).ToMiners().GenerateNewTerm(4000),
                MinersList = {stubMiners.Select(m => Address.FromPublicKey(m.PublicKey))}
            };

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GenerateConsensusTransactions.ToString(),
                initialMiner,
                new BlockHeader {Index = 1},
                new DPoSExtraInformation
                {
                    NewTerm = initialInformation.NewTerm,

                });
            var initialTransactions = GetReturnData().DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            var initialTransaction = initialTransactions.Transactions.First();

            var miningTime = initialInformation.NewTerm.FirstRound.RealTimeMinersInfo[miner.PublicKey.ToHex()]
                .ExpectedMiningTime;

            // Pretend one node receive a block from network, then he get the initialInformation.

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, ConsensusMethod.ValidateConsensus.ToString(),
                miner, initialInformation.ToByteArray());
            var validationResult = GetReturnData().DeserializeToPbMessage<ValidationResult>();

            Assert.True(validationResult?.Success);

            _contracts.ExecuteTransaction(initialTransaction, miner);

            // This node query when he can do mining.

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GetCountingMilliseconds.ToString(),
                miner, Timestamp.FromDateTime(DateTime.UtcNow));
            var distance1 = GetReturnData().DeserializeToInt32();

            Assert.True(distance1 > 100);

            // This node query in expected mining time.

            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GetCountingMilliseconds.ToString(),
                miner, miningTime);
            var distance2 = GetReturnData().DeserializeToInt32();

            Assert.True(distance2 == 0);

            // Get new consensus information (to publish out value).

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GetNewConsensusInformation.ToString(),
                miner, new DPoSExtraInformation
                {
                    HashValue = outValue
                });
            var consensusInformation = GetReturnData().DeserializeToPbMessage<DPoSInformation>();

            Assert.True(consensusInformation.CurrentRound.RealTimeMinersInfo[miner.PublicKey.ToHex()].OutValue ==
                        outValue);

            // Get transaction to update State Database.
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GenerateConsensusTransactions.ToString(),
                initialMiner,
                new BlockHeader {Index = 2},
                new DPoSExtraInformation
                {
                    ToPackage = new ToPackage
                    {
                        OutValue = outValue,
                        Signature = Hash.Generate()
                    }
                });
            var normalTransactions = GetReturnData().DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(normalTransactions);
            var normalTransaction = normalTransactions.Transactions.First();

            Assert.True(normalTransaction.MethodName == "PackageOutValue");
        }

        private ByteString GetReturnData()
        {
            return _contracts.TransactionContext.Trace.RetVal?.Data;
        }
    }
}