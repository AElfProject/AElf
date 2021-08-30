using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.CodeCheck.Application;

namespace AElf.Kernel.CodeCheck
{
    /// <summary>
    /// Maybe not useful.
    /// </summary>
    public class ContractDeployedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        public ILogger<ContractDeployedLogEventProcessor> Logger { get; set; }

        public ContractDeployedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ICheckedCodeHashProvider checkedCodeHashProvider)
        {
            _checkedCodeHashProvider = checkedCodeHashProvider;
            _smartContractAddressService = smartContractAddressService;

            Logger = NullLogger<ContractDeployedLogEventProcessor>.Instance;
        }

        public override Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return Task.FromResult(InterestedEvent);

            var address = _smartContractAddressService.GetZeroSmartContractAddress();
            if (address == null) return null;

            InterestedEvent = GetInterestedEvent<ContractDeployed>(address);

            return Task.FromResult(InterestedEvent);
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var eventData = new ContractDeployed();
            eventData.MergeFrom(logEvent);

            await _checkedCodeHashProvider.AddCodeHashAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, eventData.CodeHash);
        }
    }
}