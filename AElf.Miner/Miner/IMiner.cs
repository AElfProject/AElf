using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.Miner.Miner
{
    public interface IMiner
    {
        void Start(ECKeyPair nodeKeyPair);

        Hash Coinbase { get; }
        
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine(int timeoutMilliseconds);
    }
}