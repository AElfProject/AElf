using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Common;

namespace AElf.Miner.Miner
{
    public interface IMiner
    {
        void Init();
        void Close();

        Address Coinbase { get; }
        
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine();
    }
}