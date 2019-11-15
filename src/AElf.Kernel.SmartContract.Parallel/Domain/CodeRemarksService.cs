using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public class CodeRemarksService : ICodeRemarksService
    {
        private readonly ICodeRemarksManager _codeRemarksManager;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public CodeRemarksService(ICodeRemarksManager codeRemarksManager,
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _codeRemarksManager = codeRemarksManager;
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        public async Task MarkUnparallelizableAsync(IChainContext chainContext, Address contractAddress)
        {
            await MarkAsync(chainContext, contractAddress, true);
        }

        public async Task MarkParallelizableAsync(IChainContext chainContext, Address contractAddress)
        {
            await MarkAsync(chainContext, contractAddress, false);
        }

        private async Task MarkAsync(IChainContext chainContext, Address contractAddress, bool nonParallelizable)
        {
            var executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, contractAddress);
            await _codeRemarksManager.SetCodeRemarksAsync(executive.ContractHash, new CodeRemarks
            {
                CodeHash = executive.ContractHash,
                NonParallelizable = nonParallelizable
            });
        }
    }
}