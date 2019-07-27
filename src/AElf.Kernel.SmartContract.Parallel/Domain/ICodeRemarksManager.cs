using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface ICodeRemarksManager
    {
        Task SetCodeRemarksAsync(Hash codeHash, CodeRemarks codeRemarks);
        Task<CodeRemarks> GetCodeRemarksAsync(Hash codeHash);
    }
}