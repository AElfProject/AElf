using System.Threading.Tasks;

namespace AElf.Kernel.Miner
{
    public interface IMiner
    {
        void Start();
        void Stop();
        
        /// <summary>
        /// mining functionality
        /// </summary>
        /// <returns></returns>
        Task<IBlock> Mine();
    }
}