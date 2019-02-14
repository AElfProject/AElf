using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ITxRefBlockValidator
    {
        Task ValidateAsync(int chainId, Transaction tx);
    }
}