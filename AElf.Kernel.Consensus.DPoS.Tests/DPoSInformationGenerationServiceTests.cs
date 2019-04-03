using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS
{
    public class DPoSInformationGenerationServiceTests : DPoSConsensusTestBase
    {
        private readonly IAccountService _accountService;
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ECKeyPair _minerKeyPair;

        public DPoSInformationGenerationServiceTests()
        {
            _minerKeyPair = CryptoHelpers.GenerateKeyPair();

            _accountService = GetRequiredService<IAccountService>();
            _consensusInformationGenerationService = GetRequiredService<IConsensusInformationGenerationService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _transactionReadOnlyExecutionService = GetRequiredService<ITransactionReadOnlyExecutionService>();
        }

        [Fact(Skip = "Need to rewrite")]
        public void GetTriggerInformation_ConsensusCommand_IsNull()
        {
            var dPoSTriggerInformation =
                (CommandInput) _consensusInformationGenerationService.GetTriggerInformation(
                    TriggerType.ConsensusCommand);
            dPoSTriggerInformation.PublicKey.ToHex().ShouldBe(_accountService.GetPublicKeyAsync().Result.ToHex());
        }

        [Fact(Skip = "Need to rewrite")]
        public void GetTriggerInformation__ConsensusCommand_UpdateValue()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.UpdateValue);

            var dPoSTriggerInformation =
                (DPoSTriggerInformation) consensusInformationGenerationService.GetTriggerInformation(TriggerType
                    .ConsensusCommand);
            dPoSTriggerInformation.RandomHash.ShouldNotBeNull();
        }

        [Fact(Skip = "Need to rewrite")]
        public void GetTriggerInformation__ConsensusCommand_NextRound()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.NextRound);

            var dPoSTriggerInformation =
                (DPoSTriggerInformation) consensusInformationGenerationService.GetTriggerInformation(TriggerType
                    .ConsensusCommand);
            var publicKey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync());
            dPoSTriggerInformation.PublicKey.ToHex().ShouldBe(publicKey.ToHex());
        }

        [Fact(Skip = "Need to rewrite")]
        public void GetTriggerInformation__ConsensusCommand_NextTerm()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.NextTerm);

            var dPoSTriggerInformation =
                (DPoSTriggerInformation) consensusInformationGenerationService.GetTriggerInformation(TriggerType
                    .ConsensusCommand);
            var publicKey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync());
            dPoSTriggerInformation.PublicKey.ToHex().ShouldBe(publicKey.ToHex());
        }

        private IConsensusInformationGenerationService GetConsensusInformationGenerationService(
            DPoSBehaviour behavior)
        {
            var information = new ConsensusControlInformation()
            {
                ConsensusCommand = new ConsensusCommand
                {
                    Hint = ByteString.CopyFrom(new DPoSHint
                    {
                        Behaviour = behavior
                    }.ToByteArray())
                }
            };

            return new DPoSInformationGenerationService(_accountService, information, _smartContractAddressService,
                _transactionReadOnlyExecutionService);
        }
    }
}