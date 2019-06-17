using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Genesis;
using AElf.Kernel;
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
    public class GenesisSmartContractDto
    {
        public byte[] Code { get; set; }
        public Hash SystemSmartContractName { get; set; }

        public SystemContractDeploymentInput.Types.SystemTransactionMethodCallList TransactionMethodCallList { get; set; } =
            new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
    }

    public interface IGenesisDeploymentsProvider
    {
        IEnumerable<SystemContractDeploymentInput> GetDeployments();
    }
    
    public interface IGenesisSmartContractDtoProvider
    {
        IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress);
    }

    public class OsBlockchainNodeContextStartDto
    {
        public int ChainId { get; set; }

        public List<GenesisSmartContractDto> InitializationSmartContracts { get; set; } =
            new List<GenesisSmartContractDto>();

        public Transaction[] InitializationTransactions { get; set; }

        public Type ZeroSmartContract { get; set; }

        public int SmartContractRunnerCategory { get; set; } = KernelConstants.DefaultRunnerCategory;
        
        public bool ContractDeploymentAuthorityRequired { get; set; }
    }

    public static class GenesisSmartContractDtoExtensions
    {
        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            Type smartContractType, Hash name = null,
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = null)
        {
            genesisSmartContracts.Add(new GenesisSmartContractDto()
            {
                Code = File.ReadAllBytes(smartContractType.Assembly.Location),
                SystemSmartContractName = name,
                TransactionMethodCallList = systemTransactionMethodCallList
            });
        }
        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            byte[] code, Hash name = null,
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = null)
        {
            genesisSmartContracts.Add(new GenesisSmartContractDto()
            {
                Code = code,
                SystemSmartContractName = name,
                TransactionMethodCallList = systemTransactionMethodCallList
            });
        }
        
//        public static void AddGenesisSmartContract<T>(this List<GenesisSmartContractDto> genesisSmartContracts,
//            Hash name = null, SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = null)
//        {
//            // TODO: Change this
//            genesisSmartContracts.AddGenesisSmartContract(typeof(T), name, systemTransactionMethodCallList);
//        }
        public static void Add(this SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList, string methodName,
            IMessage input)
        {
            systemTransactionMethodCallList.Value.Add(new SystemContractDeploymentInput.Types.SystemTransactionMethodCall()
            {
                MethodName = methodName,
                Params = input?.ToByteString() ?? ByteString.Empty
            });
        }

        public static void Add(this SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList, string methodName,
            ByteString input)
        {
            systemTransactionMethodCallList.Value.Add(
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCall()
                {
                    MethodName = methodName,
                    Params = input ?? ByteString.Empty
                });
        }

        public static void AddGenesisSmartContract(this List<GenesisSmartContractDto> genesisSmartContracts,
            byte[] code, Hash name, Action<SystemContractDeploymentInput.Types.SystemTransactionMethodCallList> action)
        {
            SystemContractDeploymentInput.Types.SystemTransactionMethodCallList systemTransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            action?.Invoke(systemTransactionMethodCallList);

            genesisSmartContracts.AddGenesisSmartContract(code, name, systemTransactionMethodCallList);
        }
    }

    public interface IOsBlockchainNodeContextService
    {
        Task<OsBlockchainNodeContext> StartAsync(OsBlockchainNodeContextStartDto dto);

        Task StopAsync(OsBlockchainNodeContext blockchainNodeContext);
    }

    public class OsBlockchainNodeContextService : IOsBlockchainNodeContextService, ITransientDependency
    {
        private IBlockchainNodeContextService _blockchainNodeContextService;
        private IAElfNetworkServer _networkServer;
        private ISmartContractAddressService _smartContractAddressService;
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
                .Select(p =>
                {
                    return GetTransactionForDeployment(p.Code, p.SystemSmartContractName, dto.SmartContractRunnerCategory,
                        p.TransactionMethodCallList);
                }));
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
            int category, SystemContractDeploymentInput.Types.SystemTransactionMethodCallList transactionMethodCallList = null)
        {
            var code = File.ReadAllBytes(contractType.Assembly.Location);

            return GetTransactionForDeployment(code, systemContractName, category, transactionMethodCallList);
        }

        private Transaction GetTransactionForDeployment(byte[] code, Hash systemContractName,
            int category, SystemContractDeploymentInput.Types.SystemTransactionMethodCallList transactionMethodCallList = null)
        {
            if (transactionMethodCallList == null)
                transactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var zeroAddress = _smartContractAddressService.GetZeroSmartContractAddress();

            return new Transaction()
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(BasicContractZero.DeploySystemSmartContract),
                // TODO: change cagtegory to 0
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
                var task = nodePlugin.ShutdownAsync();
            }
        }
    }
}