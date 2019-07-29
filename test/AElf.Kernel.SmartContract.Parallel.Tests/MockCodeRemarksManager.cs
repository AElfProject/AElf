using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockCodeRemarksManager : ICodeRemarksManager
    {
        public static bool NonParallelizable = false;

        public Task SetCodeRemarksAsync(Hash codeHash, CodeRemarks codeRemarks)
        {
            throw new System.NotImplementedException();
        }

        public async Task<CodeRemarks> GetCodeRemarksAsync(Hash codeHash)
        {
            return await Task.FromResult(new CodeRemarks
            {
                CodeHash = codeHash,
                NonParallelizable = NonParallelizable
            });
        }
    }
}