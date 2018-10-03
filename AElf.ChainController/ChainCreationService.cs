using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using NLog;

namespace AElf.ChainController
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IChainService _chainService;
        private readonly ISmartContractService _smartContractService;
        private readonly ILogger _logger;

        public ChainCreationService(IChainService chainService, ISmartContractService smartContractService, ILogger logger)
        {
            _chainService = chainService;
            _smartContractService = smartContractService;
            _logger = logger;
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
                var blockchain = _chainService.GetBlockChain(chainId);
                await blockchain.AddBlocksAsync(new List<IBlock> {builder.Block});
                var chain = new Chain
                {
                    GenesisBlockHash = await blockchain.GetCurrentBlockHashAsync(),
                    Id = chainId
                };
                return chain;
            }
            catch (Exception e)
            {
                _logger.Error("CreateNewChainAsync Error: " + e);
                return null;
            }
        }

        public Address GenesisContractHash(Hash chainId, SmartContractType contractType)
        {
            return Address.FromBytes(chainId.CalculateHashWith(contractType.ToString()));
//            return new Hash(chainId.CalculateHashWith(contractType.ToString())).ToAccount();
        }
    }
}