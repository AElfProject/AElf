using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.Miner.Miner
{
    public interface IMiner
    {
        void Init(ECKeyPair _nodeKeyPair);
        
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced</returns>
        Task<IBlock> Mine();
    }
}