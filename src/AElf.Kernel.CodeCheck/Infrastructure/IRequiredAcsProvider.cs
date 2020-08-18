using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using AElf.Types;

namespace AElf.Kernel.CodeCheck.Infrastructure
{
    public interface IRequiredAcsProvider
    {
        Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight);
    }
}