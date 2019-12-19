using System.Collections.Generic;
using System.Threading.Tasks;
namespace AElf.Kernel.SmartContract.Application
{
    public interface ICalculateAlgorithmService
    {
        ICalculateAlgorithmContext CalculateAlgorithmContext { get; }
        void AddAlgorithmByBlock(BlockIndex blockIndex, IList<ICalculateWay> funcList);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
        Task<long> CalculateAsync(int count);
    }
    public interface ICalculateAlgorithmContext
    {
        int CalculateFeeTypeEnum { get; set; }
        BlockIndex BlockIndex { get; set; }
    }
}