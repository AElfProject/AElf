using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using AElf.SmartContract;
using AElf.Kernel;

namespace AElf.ChainController
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly ISmartContractService _smartContractService;

        public ChainCreationService(IChainManager chainManager, IBlockManager blockManager, ISmartContractService smartContractService)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _smartContractService = smartContractService;
        }

        /// <summary>
        /// Creates a new chain with the provided ChainId and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="chainId">The new chain id which will be derived from the creator address.</param>
        /// <param name="smartContractRegistration">The smart contract registration containing the code of the SmartContractZero.</param>
        public async Task<IChain> CreateNewChainAsync(Hash chainId, List<SmartContractRegistration> smartContractRegistration)
        {
            try
            {
                // TODO: Centralize this function in Hash class
                // SmartContractZero address can be derived from ChainId
                foreach (var reg in smartContractRegistration)
                {
                    var contractAddress = GenesisContractHash(chainId, (SmartContractType) reg.Type);
                    await _smartContractService.DeployContractAsync(chainId, contractAddress, reg, true);
                }
                
                var builder = new GenesisBlockBuilder();
                builder.Build(chainId);

                // add block to storage
                await _blockManager.AddBlockAsync(builder.Block);
                var chain = await _chainManager.AddChainAsync(chainId, builder.Block.GetHash());
                await _chainManager.AppendBlockToChainAsync(builder.Block);
                
                return chain;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public Hash GenesisContractHash(Hash chainId, SmartContractType contractType)
        {
            return new Hash(chainId.CalculateHashWith(contractType.ToString())).ToAccount();
        }
    }
}