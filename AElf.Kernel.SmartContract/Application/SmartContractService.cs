using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Contexts;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove _executivePools, _contractHashs, change ISingletonDependency to ITransientDependency
    public class SmartContractService : ISmartContractService, ISingletonDependency
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools =
            new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();

        private readonly IStateProviderFactory _stateProviderFactory;
        private readonly IFunctionMetadataService _functionMetadataService;
        private readonly IBlockchainService _chainService;

        public SmartContractService(ISmartContractManager smartContractManager,
            ISmartContractRunnerContainer smartContractRunnerContainer, IStateProviderFactory stateProviderFactory,
            IFunctionMetadataService functionMetadataService, IBlockchainService chainService)
        {
            _smartContractManager = smartContractManager;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _stateProviderFactory = stateProviderFactory;
            _functionMetadataService = functionMetadataService;
            _chainService = chainService;
        }

        public async Task DeployZeroContractAsync(int chainId, SmartContractRegistration registration)
        {
            registration.ContractHash = Hash.FromMessage(ContractHelpers.GetGenesisBasicContractAddress(chainId));

            await _smartContractManager.InsertAsync(registration);
        }



        /// <inheritdoc/>
        public async Task DeployContractAsync(int chainId, Address contractAddress,
            SmartContractRegistration registration, bool isPrivileged)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(registration.Category);
            runner.CodeCheck(registration.ContractBytes.ToByteArray(), isPrivileged);

            //Todo New version metadata handle it
//            var contractType = runner.GetContractType(registration);
//            var contractTemplate = runner.ExtractMetadata(contractType);
//            await _functionMetadataService.DeployContract(chainId, contractAddress, contractTemplate);

            await _smartContractManager.InsertAsync(registration);
        }

        public async Task UpdateContractAsync(int chainId, Address contractAddress,
            SmartContractRegistration newRegistration, bool isPrivileged)
        {
            // get runner
            var runner = _smartContractRunnerContainer.GetRunner(newRegistration.Category);
            runner.CodeCheck(newRegistration.ContractBytes.ToByteArray(), isPrivileged);

            //Todo New version metadata handle it
//            var oldRegistration = await GetContractByAddressAsync(chainId, contractAddress);
//            var oldContractType = runner.GetContractType(oldRegistration);
//            var oldContractTemplate = runner.ExtractMetadata(oldContractType);
//
//            var newContractType = runner.GetContractType(newRegistration);
//            var newContractTemplate = runner.ExtractMetadata(newContractType);
//            await _functionMetadataService.UpdateContract(chainId, contractAddress, newContractTemplate,
//                oldContractTemplate);

            await _smartContractManager.InsertAsync(newRegistration);
        }

//        public async Task<IMessage> GetAbiAsync(int chainId, Address account)
//        {
//            var reg = await GetContractByAddressAsync(chainId, account);
//            return GetAbiAsync(reg);
//        }

        /// <inheritdoc/>
//        public async Task<IEnumerable<string>> GetInvokingParams(Hash chainId, Transaction transaction)
//        {
//            var reg = await GetContractByAddressAsync(chainId, transaction.To);
//            var abi = (Module) GetAbiAsync(reg);
//            
//            // method info 
//            var methodInfo = GetContractType(reg).GetMethod(transaction.MethodName);
//            var parameters = ParamsPacker.Unpack(transaction.Params.ToByteArray(),
//                methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
//            // get method in abi
//            var method = abi.Methods.First(m => m.Name.Equals(transaction.MethodName));
//            
//            // deserialize
//            return method.DeserializeParams(parameters);
//        }
        
//        private IMessage GetAbiAsync(SmartContractRegistration reg)
//        {
//            var runner = _smartContractRunnerContainer.GetRunner(reg.Category);
//            return runner.GetAbi(reg);
//        }
//

//
//        
//        
//


//

//        private Type GetContractType(SmartContractRegistration registration)
//        {
//            var runner = _smartContractRunnerContainer.GetRunner(registration.Category);
//            if (runner == null)
//            {
//                throw new NotSupportedException($"Runner for category {registration.Category} is not registered.");
//            }
//
//            return runner.GetContractType(registration);
//        }

    }
}