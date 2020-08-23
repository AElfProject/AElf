using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.AEPoW.Application
{
    public class AEPoWTriggerInformationProvider : ITriggerInformationProvider
    {
        private readonly IAccountService _accountService;

        private ByteString Pubkey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public ILogger<AEPoWTriggerInformationProvider> Logger { get; set; }

        public AEPoWTriggerInformationProvider(IAccountService accountService)
        {
            _accountService = accountService;
            Logger = NullLogger<AEPoWTriggerInformationProvider>.Instance;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return new BytesValue{Value = Pubkey};
        }

        public BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes)
        {
            return new BytesValue{Value = Pubkey};
        }

        public BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes)
        {
            return new BytesValue{Value = Hash.Empty.ToByteString()};
        }
    }
}