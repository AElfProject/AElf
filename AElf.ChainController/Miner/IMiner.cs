using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface IMiner
    {
        void Start(ECKeyPair nodeKeyPair);
//        void Stop();

        Hash Coinbase { get; }
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine(int timeoutMilliseconds);
    }
}