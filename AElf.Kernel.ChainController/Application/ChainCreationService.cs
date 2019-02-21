using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Helpers;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Type = System.Type;

namespace AElf.Kernel.ChainController.Application
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly ISystemContractService _systemContractService;
        private readonly IBlockchainService _blockchainService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly IBlockExecutingService _blockExecutingService;
        public ILogger<ChainCreationService> Logger { get; set; }

        public ChainCreationService(ISystemContractService systemContractService, IBlockchainService blockchainService,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IBlockExecutingService blockExecutingService)
        {
            _systemContractService = systemContractService;
            _blockchainService = blockchainService;
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _blockExecutingService = blockExecutingService;
            Logger = NullLogger<ChainCreationService>.Instance;
        }

        /// <summary>
        /// Creates a new chain with the provided ChainId and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="chainId">The new chain id which will be derived from the creator address.</param>
        /// <param name="smartContractRegistrationList">The smart contract registration containing the code of the SmartContractZero.</param>
        public async Task<Chain> CreateNewChainAsync(int chainId,
            List<SmartContractRegistration> smartContractRegistrationList)
        {
            try
            {
                var contractZero = Address.BuildContractAddress(chainId, 0);
                var contractDeployments = smartContractRegistrationList.Select(x => new Transaction()
                {
                    From = contractZero,
                    To = contractZero,
                    MethodName = nameof(ISmartContractZero.DeploySmartContract),
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(x.Category, x.Code))
                });

                var blockHeader = new BlockHeader
                {
                    Height = GlobalConfig.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis,
                    ChainId = chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                var block = await _blockExecutingService.ExecuteBlockAsync(chainId, blockHeader, contractDeployments);

                await _blockchainService.CreateChainAsync(chainId, block);
                return await _blockchainService.GetChainAsync(chainId);
            }
            catch (Exception e)
            {
                Logger.LogError("CreateNewChainAsync Error: " + e);
                throw;
            }
        }
    }
}