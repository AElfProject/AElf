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
        private readonly IChainService _chainService;
        private readonly ISmartContractService _smartContractService;

        public ChainCreationService(IChainService chainService, ISmartContractService smartContractService)
        {
            _chainService = chainService;
            _smartContractService = smartContractService;
        }

        /// <summary>
        /// Creates a new chain with the provided ChainId and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="chainId">The new chain id which will be derived from the creator address.</param>
        /// <param name="smartContractRegistration">Thec smart contract registration containing the code of the SmartContractZero.</param>
        public async Task<IChain> CreateNewChainAsync(Hash chainId, SmartContractRegistration smartContractRegistration)
        {
            try
            {
                // TODO: Centralize this function in Hash class
                // SmartContractZero address can be derived from ChainId
                var contractAddress = GenesisContractHash(chainId);
                await _smartContractService.DeployContractAsync(chainId, contractAddress, smartContractRegistration,
                    true);
                var builder = new GenesisBlockBuilder();
                builder.Build(chainId);

                // add block to storage
                var blockchain = _chainService.GetBlockChain(chainId);
                await blockchain.AddBlocksAsync(new List<IBlock>() {builder.Block});
                var chain = new Chain()
                {
                    GenesisBlockHash = await blockchain.GetCurrentBlockHashAsync(),
                    Id = chainId
                };
                return chain;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public Hash GenesisContractHash(Hash chainId)
        {
            return new Hash(chainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();
        }
    }
}