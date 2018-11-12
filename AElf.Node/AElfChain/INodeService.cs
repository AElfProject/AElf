using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Node.AElfChain
{
    public interface INodeService
    {
        void Initialize(NodeConfiguration conf);
        bool Start();
        void Stop();
        bool IsDPoSAlive();
        bool IsForked();

        Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count);

        Task<Block> GetBlockFromHash(byte[] hash);
        Task<Block> GetBlockAtHeight(int height);
        Task<int> GetCurrentBlockHeightAsync();
    }
}