using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Newtonsoft.Json;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IChainStatusAppService : IApplicationService
    {
        Task<ChainStatusDto> GetChainStatusAsync();
    }

    public class ChainStatusAppService : IChainStatusAppService
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        private readonly IBlockchainService _blockchainService;
        
        public ChainStatusAppService(ISmartContractAddressService smartContractAddressService,
            IBlockchainService blockchainService)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainService = blockchainService;
        }
        
        
        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns></returns>
        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();
     
            var chain = await _blockchainService.GetChainAsync();
            var branches = JsonConvert.DeserializeObject<Dictionary<string, long>>(chain.Branches.ToString());
            var formattedNotLinkedBlocks = new List<NotLinkedBlockDto>();

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var block = await _blockchainService.GetBlockAsync(Hash.LoadBase64(notLinkedBlock.Value));
                formattedNotLinkedBlocks.Add(new NotLinkedBlockDto
                    {
                        BlockHash = block.GetHash().ToHex(),
                        Height = block.Height,
                        PreviousBlockHash = block.Header.PreviousBlockHash.ToHex()
                    }
                );
            }

            return new ChainStatusDto()
            {
                ChainId = ChainHelpers.ConvertChainIdToBase58(chain.Id),
                GenesisContractAddress = basicContractZero?.GetFormatted(),
                Branches = branches,
                NotLinkedBlocks = formattedNotLinkedBlocks,
                LongestChainHeight = chain.LongestChainHeight,
                LongestChainHash = chain.LongestChainHash?.ToHex(),
                GenesisBlockHash = chain.GenesisBlockHash.ToHex(),
                LastIrreversibleBlockHash = chain.LastIrreversibleBlockHash?.ToHex(),
                LastIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight,
                BestChainHash = chain.BestChainHash?.ToHex(),
                BestChainHeight = chain.BestChainHeight
            };
        }

    }
}