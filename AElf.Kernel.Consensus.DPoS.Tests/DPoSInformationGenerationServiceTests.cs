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
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS
{
    public class DPoSInformationGenerationServiceTests : DPoSConsensusTestBase
    {
        public DPoSInformationGenerationServiceTests()
        {
            _minerKeyPair = CryptoHelpers.GenerateKeyPair();
            _dpoSOptions = new DPoSOptions
            {
                InitialMiners = new List<string>
                {
                    _minerKeyPair.PublicKey.ToHex()
                },
                InitialTermNumber = 1,
                MiningInterval = 2000,
                IsBootMiner = true
            };
            _accountService = GetRequiredService<IAccountService>();
            _consensusInformationGenerationService = GetRequiredService<IConsensusInformationGenerationService>();
        }

        private readonly DPoSOptions _dpoSOptions;
        private readonly IAccountService _accountService;
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly ECKeyPair _minerKeyPair;

        private IConsensusInformationGenerationService GetConsensusInformationGenerationService(
            DPoSBehaviour behavior)
        {
            var information = new ConsensusControlInformation
            {
                ConsensusCommand = new ConsensusCommand
                {
                    Hint = ByteString.CopyFrom(new DPoSHint
                    {
                        Behaviour = behavior
                    }.ToByteArray())
                }
            };

            var option = Options.Create(_dpoSOptions);
            return new DPoSInformationGenerationService(option, _accountService, information);
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_Exception()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.Invalid);
            Should.Throw<InvalidOperationException>(() =>
            {
                consensusInformationGenerationService.GetTriggerInformation();
            });
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_InitialConsensus()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.InitialConsensus);

            var dPoSTriggerInformation = consensusInformationGenerationService.GetTriggerInformation();
            dPoSTriggerInformation.Miners.Count.ShouldBeGreaterThanOrEqualTo(1);
            dPoSTriggerInformation.Miners[0].ShouldBe(_minerKeyPair.PublicKey.ToHex());
            dPoSTriggerInformation.MiningInterval.ShouldBe(2000);
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_NextRound()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.NextRound);

            var dPoSTriggerInformation = consensusInformationGenerationService.GetTriggerInformation();
            var publicKey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync());
            dPoSTriggerInformation.PublicKey.ShouldBe(publicKey.ToHex());
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_NextTerm()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.NextTerm);

            var dPoSTriggerInformation = consensusInformationGenerationService.GetTriggerInformation();
            var publicKey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync());
            dPoSTriggerInformation.PublicKey.ShouldBe(publicKey.ToHex());
        }

        [Fact]
        public void GetTriggerInformation__ConsensusCommand_UpdateValue()
        {
            var consensusInformationGenerationService =
                GetConsensusInformationGenerationService(DPoSBehaviour.UpdateValue);

            var dPoSTriggerInformation = consensusInformationGenerationService.GetTriggerInformation();
            dPoSTriggerInformation.CurrentInValue.ShouldNotBeNull();
            dPoSTriggerInformation.PreviousInValue.ShouldBe(Hash.Empty);

            var dPoSTriggerInformation1 = consensusInformationGenerationService.GetTriggerInformation();
            dPoSTriggerInformation1.PreviousInValue.ShouldBe(dPoSTriggerInformation.CurrentInValue);
        }

        [Fact]
        public void GetTriggerInformation_ConsensusCommand_IsNull()
        {
            var dPoSTriggerInformation = _consensusInformationGenerationService.GetTriggerInformation();
            dPoSTriggerInformation.PublicKey.ShouldBe(_accountService.GetPublicKeyAsync().Result.ToHex());
            dPoSTriggerInformation.IsBootMiner.ShouldBeTrue();
            dPoSTriggerInformation.Miners.Count.ShouldBe(0);
        }
    }
}