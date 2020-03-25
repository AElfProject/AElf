using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Application
{
    public interface IInvalidDataProcessingService
    {
        Task ProcessInvalidTransactionAsync(Hash transactionId);
    }
}