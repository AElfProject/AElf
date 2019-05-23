using Acs4;
using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Volo.Abp.Threading;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class AEDPoSTriggerInformationProvider : ITriggerInformationProvider
    {
        private readonly IAccountService _accountService;

        private Hash _latestRandomHash = Hash.Empty;

        private ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public ILogger<AEDPoSTriggerInformationProvider> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return new BytesValue {Value = PublicKey};
        }

        public BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes)
        {
            if (consensusCommandBytes == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            var command = consensusCommandBytes.ToConsensusCommand();
            var behaviour = command.Hint.ToAElfConsensusHint().Behaviour;
            if (behaviour == AElfConsensusBehaviour.UpdateValue ||
                behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    RandomHash = GetRandomHash(command),
                    PreviousRandomHash = _latestRandomHash,
                    Behaviour = behaviour
                }.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = behaviour
            }.ToBytesValue();
        }

        public BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes)
        {
            if (consensusCommandBytes == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            var command = consensusCommandBytes.ToConsensusCommand();
            var behaviour = command.Hint.ToAElfConsensusHint().Behaviour;
            if (behaviour == AElfConsensusBehaviour.UpdateValue ||
                behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                var trigger = new AElfConsensusTriggerInformation
                {
                    PublicKey = PublicKey,
                    RandomHash = GetRandomHash(command),
                    PreviousRandomHash = _latestRandomHash,
                    Behaviour = behaviour
                };

                var newRandomHash = GetRandomHash(command);
                _latestRandomHash = newRandomHash;

                return trigger.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = behaviour
            }.ToBytesValue();
        }

        /// <summary>
        /// For generating in_value.
        /// </summary>
        /// <returns></returns>
        private Hash GetRandomHash(ConsensusCommand consensusCommand)
        {
            var data = Hash.FromRawBytes(consensusCommand.NextBlockMiningLeftMilliseconds
                .DumpByteArray());
            var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.DumpByteArray()));
            return Hash.FromRawBytes(bytes);
        }
    }
}