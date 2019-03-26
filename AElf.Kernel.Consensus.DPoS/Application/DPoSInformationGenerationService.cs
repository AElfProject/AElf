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
            ConsensusControlInformation controlInformation, ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _accountService = accountService;
            _controlInformation = controlInformation;
            _smartContractAddressService = smartContractAddressService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;

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
                        RandomHash = RandomHash,
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
                GetTriggerInformation(), nextMiningTime)).ToByteArray();
        }
    }
}