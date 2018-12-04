using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Common;

namespace AElf.Miner.Miner
{
    public interface IMiner
    {
        Address Coinbase { get; }
        
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine();
    }
}