using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.CodeCheck.Application
{
    public interface ICheckedCodeHashProvider
    {
        Task AddCodeHashAsync(BlockIndex blockIndex, Hash codeHash);
        bool IsCodeHashExists(BlockIndex blockIndex, Hash codeHash);
        Task RemoveCodeHashAsync(BlockIndex blockIndex, long libHeight);
    }

    internal class CheckedCodeHashProvider : BlockExecutedDataBaseProvider<ContractCodeHashMap>,
        ICheckedCodeHashProvider, ISingletonDependency
    {
        public CheckedCodeHashProvider(
            ICachedBlockchainExecutedDataService<ContractCodeHashMap> cachedBlockchainExecutedDataService) :
            base(cachedBlockchainExecutedDataService)
        {

        }

        public async Task AddCodeHashAsync(BlockIndex blockIndex, Hash codeHash)
        {
            var codeHashMap = GetBlockExecutedData(blockIndex);
            codeHashMap.TryAdd(blockIndex.BlockHeight, codeHash);
            await AddBlockExecutedDataAsync(blockIndex, codeHashMap);
        }

        public bool IsCodeHashExists(BlockIndex blockIndex, Hash codeHash)
        {
            var codeHashMap = GetBlockExecutedData(blockIndex);
            return codeHashMap.ContainsValue(codeHash);
        }

        public async Task RemoveCodeHashAsync(BlockIndex blockIndex, long libHeight)
        {
            var codeHashMap = GetBlockExecutedData(blockIndex);
            codeHashMap.RemoveValuesBeforeLibHeight(libHeight);
            await AddBlockExecutedDataAsync(blockIndex, codeHashMap);
        }

        protected override string GetBlockExecutedDataName()
        {
            return nameof(ContractCodeHashMap);
        }
    }
}