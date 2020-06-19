using System.Threading.Tasks;
using AElf.Kernel.Account.Infrastructure;

namespace AElf.ContractTestKit.AEDPoSExtension
{
    public interface ITestDataProvider : IBlockTimeProvider, ITransactionListProvider,
        IAElfAsymmetricCipherKeyPairProvider
    {
        Task<long> GetCurrentBlockHeight();
    }
}