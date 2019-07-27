using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public class CodeRemarksManager : ICodeRemarksManager
    {
        private readonly IBlockchainStore<CodeRemarks> _codeRemarksStore;

        public CodeRemarksManager(IBlockchainStore<CodeRemarks> codeRemarksStore)
        {
            _codeRemarksStore = codeRemarksStore;
        }

        public async Task SetCodeRemarksAsync(Hash codeHash, CodeRemarks codeRemarks)
        {
            await _codeRemarksStore.SetAsync(codeHash.ToStorageKey(), codeRemarks);
        }

        public async Task<CodeRemarks> GetCodeRemarksAsync(Hash codeHash)
        {
            return await _codeRemarksStore.GetAsync(codeHash.ToStorageKey());
        }
    }
}