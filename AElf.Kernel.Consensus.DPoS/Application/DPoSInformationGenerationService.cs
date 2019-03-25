using System;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly IAccountService _accountService;
        private readonly ConsensusControlInformation _controlInformation;

        private ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        private DPoSHint Hint => DPoSHint.Parser.ParseFrom(_controlInformation.ConsensusCommand.Hint);
        
        private Hash RandomHash
        {
            get
            {
                var data = Hash.FromRawBytes(_controlInformation.ConsensusCommand.NextBlockMiningLeftMilliseconds.DumpByteArray());
                var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.DumpByteArray()));
                return Hash.FromRawBytes(bytes);
            }
        }

        public ILogger<DPoSInformationGenerationService> Logger { get; set; }

        public DPoSInformationGenerationService(IAccountService accountService,
            ConsensusControlInformation controlInformation)
        {
            _accountService = accountService;
            _controlInformation = controlInformation;

            Logger = NullLogger<DPoSInformationGenerationService>.Instance;
        }

        public IMessage GetTriggerInformation()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new DPoSTriggerInformation
                {
                    PublicKey = PublicKey
                };
            }

            switch (Hint.Behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                        RandomHash = RandomHash
                    };
                case DPoSBehaviour.NextRound:
                case DPoSBehaviour.NextTerm:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                    };
                case DPoSBehaviour.Invalid:
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IMessage ParseConsensusTriggerInformation(byte[] consensusTriggerInformation)
        {
            return DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);
        }
    }
}