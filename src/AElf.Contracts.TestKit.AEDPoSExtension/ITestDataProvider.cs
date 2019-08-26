using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel.Account.Infrastructure;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public interface ITestDataProvider : IBlockTimeProvider, ITransactionListProvider,
        IAElfAsymmetricCipherKeyPairProvider
    {
        Task<long> GetCurrentBlockHeight();
    }
}