using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Kernel.SmartContract.Application
{
    public class HostSmartContractBridgeContextService : IHostSmartContractBridgeContextService
    {
        private readonly ISmartContractBridgeService _smartContractBridgeService;

        public HostSmartContractBridgeContextService(ISmartContractBridgeService smartContractBridgeService)
        {
            _smartContractBridgeService = smartContractBridgeService;
        }


        public IHostSmartContractBridgeContext Create(ISmartContractContext smartContractContext)
        {
            var context = new HostSmartContractBridgeContext(_smartContractBridgeService);

            context.SmartContractContext = smartContractContext;
            return context;
        }
    }
}