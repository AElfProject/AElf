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
            
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress, ConsensusMethod.GetCountingMilliseconds.ToString(),
                initialMiner, Timestamp.FromDateTime(DateTime.UtcNow));
            var countingMilliseconds = _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();
            Assert.Equal(10_000, countingMilliseconds);
            
            // Let's say 10s has passed, this node begin to acquire consensus information.
            
            _contracts.ExecuteAction(_contracts.ConsensusContractAddress,
                ConsensusMethod.GetNewConsensusInformation.ToString(),
                initialMiner, new DPoSExtraInformation
                {
                    InitialMiners = {stubMiners.Select(m => m.PublicKey.ToHex())}
                });
            var consensusInformation =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<DPoSInformation>();
            
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
            var initialTransactions =
                _contracts.TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<TransactionList>();
            Assert.NotNull(initialTransactions);
            var initialTransaction = initialTransactions.Transactions.First();

            Assert.True(initialTransaction.MethodName == "InitialTerm");
        }
    }
}