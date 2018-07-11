using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;
using AElf.Services.Execution;
using AElf.Kernel;

namespace AElf.Services.Miner
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