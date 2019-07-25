using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Genesis;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Domain;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Node.Application
{
    public class OsBlockchainNodeContextService : IOsBlockchainNodeContextService, ITransientDependency
    {
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IAElfNetworkServer _networkServer;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IServiceContainer<INodePlugin> _nodePlugins;

        public OsBlockchainNodeContextService(IBlockchainNodeContextService blockchainNodeContextService,
            IAElfNetworkServer networkServer, ISmartContractAddressService smartContractAddressService,
            IServiceContainer<INodePlugin> nodePlugins)
        {
            _blockchainNodeContextService = blockchainNodeContextService;
            _networkServer = networkServer;
            _smartContractAddressService = smartContractAddressService;
            _nodePlugins = nodePlugins;
        }

        public async Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto)
        {
            var transactions = new List<Transaction>();

            transactions.Add(GetTransactionForDeployment(dto.ZeroSmartContract, Hash.Empty,
                dto.SmartContractRunnerCategory));

            transactions.AddRange(dto.InitializationSmartContracts
                .Select(p => GetTransactionForDeployment(p.Code, p.SystemSmartContractName,
                    dto.SmartContractRunnerCategory,
                    p.TransactionMethodCallList)));

            if (dto.InitializationTransactions != null)
                transactions.AddRange(dto.InitializationTransactions);

            // Add transaction for initialization
            transactions.Add(GetTransactionForGenesisOwnerInitialization(dto));
            
            var blockchainNodeContextStartDto = new BlockchainNodeContextStartDto()
            {
                ChainId = dto.ChainId,
                ZeroSmartContractType = dto.ZeroSmartContract,
                Transactions = transactions.ToArray()
            };

            var context = new OsBlockchainNodeContext
            {
                BlockchainNodeContext =
                    await _blockchainNodeContextService.StartAsync(blockchainNodeContextStartDto),
                AElfNetworkServer = _networkServer
            };

            await _networkServer.StartAsync();

            foreach (var nodePlugin in _nodePlugins)
            {
                await nodePlugin.StartAsync(dto.ChainId);
            }

            return context;
        }

        private Transaction GetTransactionForDeployment(Type contractType, Hash systemContractName,
            int category,
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList transactionMethodCallList = null)
        {
            var code = File.ReadAllBytes(contractType.Assembly.Location);

            return GetTransactionForDeployment(code, systemContractName, category, transactionMethodCallList);
        }

        private Transaction GetTransactionForDeployment(byte[] code, Hash systemContractName,
            int category,
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList transactionMethodCallList = null)
        {
            if (transactionMethodCallList == null)
                transactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var zeroAddress = _smartContractAddressService.GetZeroSmartContractAddress();

            return new Transaction()
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(BasicContractZero.DeploySystemSmartContract),
                Params = new SystemContractDeploymentInput()
                {
                    Name = systemContractName,
                    Category = category,
                    Code = ByteString.CopyFrom(code),
                    TransactionMethodCallList = transactionMethodCallList
                }.ToByteString()
            };
        }
        
        private Transaction GetTransactionForGenesisOwnerInitialization(OsBlockchainNodeContextStartDto dto)
        {
            var zeroAddress = _smartContractAddressService.GetZeroSmartContractAddress();
            return new Transaction
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(BasicContractZero.Initialize),
                Params = new InitializeInput
                    {ContractDeploymentAuthorityRequired = dto.ContractDeploymentAuthorityRequired}.ToByteString()
            };
        }

        public async Task StopAsync(OsBlockchainNodeContext blockchainNodeContext)
        {
            await _networkServer.StopAsync(false);

            await _blockchainNodeContextService.StopAsync(blockchainNodeContext.BlockchainNodeContext);

            foreach (var nodePlugin in _nodePlugins)
            {
                var _ = nodePlugin.ShutdownAsync();
            }
        }
    }
}