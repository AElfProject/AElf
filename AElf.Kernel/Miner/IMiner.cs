using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Types;

namespace AElf.Kernel.Miner
{
    public interface IMiner
    {
        void Start(ECKeyPair nodeKeyPair, IGrouper grouper );
        void Stop();

        Hash Coinbase { get; }
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine();
    }
}