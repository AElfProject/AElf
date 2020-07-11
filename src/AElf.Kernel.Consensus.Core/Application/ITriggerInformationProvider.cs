using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.Application
{
    // ReSharper disable UnusedParameter.Global
    public interface ITriggerInformationProvider
    {
        BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes);
        BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes);
        BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes);
    }

    public class DefaultTriggerInformationProvider : ITriggerInformationProvider
    {
        private readonly IAccountService _accountService;

        private BytesValue Pubkey => new BytesValue
        {
            Value = ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync))
        };

        public DefaultTriggerInformationProvider(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return Pubkey;
        }

        public BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes)
        {
            return Pubkey;
        }

        public BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes)
        {
            return Pubkey;
        }
    }
}