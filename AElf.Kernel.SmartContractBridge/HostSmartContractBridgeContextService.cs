namespace AElf.Kernel.SmartContractBridge
{
    public class HostSmartContractBridgeContextService : IHostSmartContractBridgeContextService
    {
        private readonly ISmartContractBridgeService _smartContractBridgeService;

        public HostSmartContractBridgeContextService(ISmartContractBridgeService smartContractBridgeService)
        {
            _smartContractBridgeService = smartContractBridgeService;
        }


        public IHostSmartContractBridgeContext Create()
        {
            return new HostSmartContractBridgeContext(_smartContractBridgeService);
        }
    }
}