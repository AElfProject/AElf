using System;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockFoundLogEventHandler : ILogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private LogEvent _interestedEvent;

        public IrreversibleBlockFoundLogEventHandler(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null) return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);

                _interestedEvent = new IrreversibleBlockFound().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public Task Handle(Block block, TransactionResult result, LogEvent log)
        {
            throw new NotImplementedException();
        }
    }
}