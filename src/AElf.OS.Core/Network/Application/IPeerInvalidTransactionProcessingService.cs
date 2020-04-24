using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Application
{
    public interface IPeerInvalidTransactionProcessingService
    {
        Task ProcessPeerInvalidTransactionAsync(Hash transactionId);
    }
}