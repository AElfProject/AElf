using System.Collections.Generic;
using System.Threading.Tasks;
namespace AElf.Kernel.SmartContract.Application
{
    public interface ICalculateAlgorithmService
    {
        ICalculateAlgorithmContext CalculateAlgorithmContext { get; }
        void AddAlgorithmByBlock(BlockIndex blockIndex, IList<ICalculateWay> funcList);
        
        //TODO: if you are a CalculateAlgorithmService, why do you need to care about fork? one class does one thing
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