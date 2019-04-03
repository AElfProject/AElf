using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly IAccountService _accountService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        private ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        private DPoSHint Hint => DPoSHint.Parser.ParseFrom(_controlInformation.ConsensusCommand.Hint);

        private readonly ConsensusControlInformation _controlInformation;

        private TriggerType _firstTriggerType;

        private Hash RandomHash
        {
            get
            {
                var data = Hash.FromRawBytes(_controlInformation.ConsensusCommand.NextBlockMiningLeftMilliseconds
                    .DumpByteArray());
                var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.DumpByteArray()));
                return Hash.FromRawBytes(bytes);
            }
        }

        private Hash _latestRandomHash = Hash.Empty;

        public ILogger<DPoSInformationGenerationService> Logger { get; set; }

        public DPoSInformationGenerationService(IAccountService accountService,
            ConsensusControlInformation controlInformation, ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _accountService = accountService;
            _controlInformation = controlInformation;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;

            Logger = NullLogger<DPoSInformationGenerationService>.Instance;
        }

        public IMessage GetTriggerInformation(TriggerType triggerType)
        {
            if (triggerType == TriggerType.ConsensusCommand)
            {
                return new CommandInput {PublicKey = PublicKey};
            }

            if(_firstTriggerType == default)
            {
                _firstTriggerType = triggerType;
            }

            if (_controlInformation.ConsensusCommand == null)
            {
                return new DPoSTriggerInformation
                {
                    PublicKey = PublicKey,
                    Behaviour = DPoSBehaviour.UpdateValue
                };
            }

            if (Hint.Behaviour == DPoSBehaviour.UpdateValue ||
                Hint.Behaviour == DPoSBehaviour.UpdateValueWithoutPreviousInValue)
            {
                var trigger = new DPoSTriggerInformation
                {
                    PublicKey = PublicKey,
                    RandomHash = RandomHash,
                    PreviousRandomHash = _latestRandomHash,
                    Behaviour = Hint.Behaviour
                };

                if (triggerType != _firstTriggerType)
                {
                    _latestRandomHash = RandomHash;
                }

                return trigger;
            }

            return new DPoSTriggerInformation
            {
                PublicKey = PublicKey,
                Behaviour = Hint.Behaviour
            };
        }

        public IMessage ParseConsensusTriggerInformation(byte[] consensusTriggerInformation)
        {
            return DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);
        }

        public async Task<T> ExecuteContractAsync<T>(IChainContext chainContext, string consensusMethodName,
            IMessage input, DateTime dateTime) where T : class, IMessage<T>, new()
        {
            var tx = new Transaction
            {
                From = Address.Generate(),
                To = _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                    .Name),
                MethodName = consensusMethodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            };

            return await _transactionReadOnlyExecutionService.ExecuteAsync<T>(chainContext, tx, dateTime);
        }

        public async Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext,
            DateTime nextMiningTime)
        {
            return (await ExecuteContractAsync<DPoSHeaderInformation>(chainContext,
                ConsensusConsts.GetInformationToUpdateConsensus,
                GetTriggerInformation(TriggerType.BlockHeaderExtraData), nextMiningTime)).ToByteArray();
        }
    }
}