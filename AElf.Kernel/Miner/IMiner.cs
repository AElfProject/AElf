using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;

namespace AElf.Kernel.Miner
{
    public interface IMiner
    {
        void Start(ECKeyPair nodeKeyPair);
        void Stop();

        Hash Coinbase { get; }
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine();
    }
}