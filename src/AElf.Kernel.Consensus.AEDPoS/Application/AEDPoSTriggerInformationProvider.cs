using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Volo.Abp.Threading;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;
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

        public ILogger<AEDPoSExtraDataParsingService> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService,
            ConsensusControlInformation controlInformation)
        {
            _accountService = accountService;
            _controlInformation = controlInformation;
        }

        public BytesValue GetTriggerInformationToGetConsensusCommand()
        {
            return new BytesValue {Value = PublicKey};
        }

        public BytesValue GetTriggerInformationToGetExtraData()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            if (Hint.Behaviour == AElfConsensusBehaviour.UpdateValue ||
                Hint.Behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    RandomHash = GetRandomHash(),
                    PreviousRandomHash = _latestRandomHash,
                    Behaviour = Hint.Behaviour
                }.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = Hint.Behaviour
            }.ToBytesValue();
        }

        public BytesValue GetTriggerInformationToGenerateConsensusTransactions()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
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

                return trigger.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = Hint.Behaviour
            }.ToBytesValue();
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