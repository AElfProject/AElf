using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using AElf.Kernel.Consensus.AElfConsensus.Application;

namespace AElf.Kernel.Consensus.AElfConsensus
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

        [Fact]
        public void GetTriggerInformation_ConsensusCommand_IsNull()
        {
            var dPoSTriggerInformation = CommandInput.Parser.ParseFrom(
                _consensusInformationGenerationService.GetTriggerInformation(
                    TriggerType.ConsensusCommand).ToByteString()
            );

            dPoSTriggerInformation.ShouldNotBeNull();
            dPoSTriggerInformation.PublicKey.ToHex().ShouldBe(_accountService.GetPublicKeyAsync().Result.ToHex());
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_UpdateValue()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.UpdateValue);

            var dPoSTriggerInformation = DPoSTriggerInformation.Parser.ParseFrom(
                consensusInformationGenerationService.GetTriggerInformation(TriggerType.BlockHeaderExtraData)
                    .ToByteString());
            dPoSTriggerInformation.RandomHash.ShouldNotBeNull();
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_NextRound()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.NextRound);

            var dPoSTriggerInformation = DPoSTriggerInformation.Parser.ParseFrom(
                consensusInformationGenerationService.GetTriggerInformation(TriggerType.BlockHeaderExtraData)
                    .ToByteString());

            var publicKey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync());
            dPoSTriggerInformation.PublicKey.ToHex().ShouldBe(publicKey.ToHex());
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_NextTerm()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.NextTerm);

            var dPoSTriggerInformation = DPoSTriggerInformation.Parser.ParseFrom(
                consensusInformationGenerationService.GetTriggerInformation(TriggerType.BlockHeaderExtraData)
                    .ToByteString());
            var publicKey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync());
            dPoSTriggerInformation.PublicKey.ToHex().ShouldBe(publicKey.ToHex());
        }

        [Fact]
        public void ParseConsensusTriggerInformation()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.UpdateValue);

            var bytes =
                consensusInformationGenerationService.GetTriggerInformation(TriggerType
                    .BlockHeaderExtraData).ToByteArray();
            var dPoSTriggerInformation = DPoSTriggerInformation.Parser.ParseFrom(bytes);

            dPoSTriggerInformation.ShouldNotBeNull();
            dPoSTriggerInformation.RandomHash.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetInformationToUpdateConsensusAsync()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.UpdateValue);
            var chainContext = new ChainContext
            {
                BlockHash = Hash.Generate(),
                BlockHeight = 1
            };
            var bytes = await consensusInformationGenerationService.GetInformationToUpdateConsensusAsync(chainContext,
                DateTime.Now.Add(TimeSpan.FromSeconds(4)));

            var dposInformation = DPoSTriggerInformation.Parser.ParseFrom(bytes);
            dposInformation.Behaviour.ShouldBe(DPoSBehaviour.UpdateValue);
            dposInformation.PublicKey.ShouldBe(ByteString.CopyFromUtf8("test"));
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

            return new AElfConsensusInformationGenerationService(_accountService, information, _smartContractAddressService,
                _transactionReadOnlyExecutionService);
        }
    }
}