using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Volo.Abp.Threading;
using AElf.Contracts.Consensus.AEDPoS;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class AEDPoSTriggerInformationProvider : ITriggerInformationProvider
    {
        private readonly IAccountService _accountService;
        private readonly ConsensusControlInformation _controlInformation;

        private Hash _latestRandomHash = Hash.Empty;

        private ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        private AElfConsensusHint Hint => AElfConsensusHint.Parser.ParseFrom(_controlInformation.ConsensusCommand.Hint);

        public ILogger<AEDPoSInformationGenerationService> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService,
            ConsensusControlInformation controlInformation)
        {
            _accountService = accountService;
            _controlInformation = controlInformation;
        }

        public CommandInput GetTriggerInformationToGetConsensusCommand()
        {
            return new CommandInput {PublicKey = PublicKey};
        }

        public AElfConsensusTriggerInformation GetTriggerInformationToGetExtraData()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                };
            }

            if (Hint.Behaviour == AElfConsensusBehaviour.UpdateValue ||
                Hint.Behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                var trigger = new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    RandomHash = GetRandomHash(),
                    PreviousRandomHash = _latestRandomHash,
                    Behaviour = Hint.Behaviour
                };

                return trigger;
            }

            return new AElfConsensusTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = Hint.Behaviour
            };
        }

        public AElfConsensusTriggerInformation GetTriggerInformationToGenerateConsensusTransactions()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                };
            }

            if (Hint.Behaviour == AElfConsensusBehaviour.UpdateValue ||
                Hint.Behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                var trigger = new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    RandomHash = GetRandomHash(),
                    PreviousRandomHash = _latestRandomHash,
                    Behaviour = Hint.Behaviour
                };

                var newRandomHash = GetRandomHash();
                Logger.LogTrace($"Update lasted random hash to {newRandomHash.ToHex()}");
                _latestRandomHash = newRandomHash;

                return trigger;
            }

            return new AElfConsensusTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = Hint.Behaviour
            };
        }

        private Hash GetRandomHash()
        {
            var data = Hash.FromRawBytes(_controlInformation.ConsensusCommand.NextBlockMiningLeftMilliseconds
                .DumpByteArray());
            var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.DumpByteArray()));
            return Hash.FromRawBytes(bytes);
        }
    }
}