using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        public ILogger<CheckedCodeHashProvider> Logger { get; set; }

        public CheckedCodeHashProvider(
            ICachedBlockchainExecutedDataService<ContractCodeHashMap> cachedBlockchainExecutedDataService) :
            base(cachedBlockchainExecutedDataService)
        {
            Logger = NullLogger<CheckedCodeHashProvider>.Instance;
        }

        public async Task AddCodeHashAsync(BlockIndex blockIndex, Hash codeHash)
        {
            Logger.LogInformation($"Added code hash: {blockIndex}");
            var codeHashMap = GetBlockExecutedData(blockIndex) ?? new ContractCodeHashMap();
            codeHashMap.TryAdd(blockIndex.BlockHeight, codeHash);
            await AddBlockExecutedDataAsync(blockIndex, codeHashMap);
        }

        public bool IsCodeHashExists(BlockIndex blockIndex, Hash codeHash)
        {
            var codeHashMap = GetBlockExecutedData(blockIndex);
            var result = codeHashMap.ContainsValue(codeHash);
            Logger.LogInformation($"Is {codeHash} exists in height {blockIndex.BlockHeight}: {result}");
            return result;
        }

        public async Task RemoveCodeHashAsync(BlockIndex blockIndex, long libHeight)
        {
            Logger.LogInformation($"Removing code hash list below height {libHeight}");
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