using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Google.Protobuf;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero : ISmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";

        private const string SMART_CONTRACT_INSTANCES = "SmartContractInstances";

        private IAccountDataProvider _accountDataProvider;

        private readonly IDictionary<IHash, ISmartContract> _smartContracts =
            new Dictionary<IHash, ISmartContract>();

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly IWorldStateManager _worldStateManager;

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory, IWorldStateManager worldStateManager)
        {
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateManager = worldStateManager;
        }

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await _worldStateManager.OfChain(dataProvider.Context.ChainId);
        }

        public async Task InvokeAsync(SmartContractInvokeContext context)
        {
            var type = typeof(SmartContractZero);
            
            // method info
            var member = type.GetMethod(context.MethodName);
            // params array
            var parameters = Parameters.Parser.ParseFrom(context.Params).Params.Select(p => p.Value()).ToArray();
            
            
            // invoke
            await (Task) member.Invoke(this, parameters);
        }

        /// <inheritdoc/>
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            //TODO: For now just hard coded to Hash.Zero
            await smartContractMap.SetAsync(reg.ContractHash, reg.Serialize());
        }
        
        
        /// <inheritdoc/>
        public async Task<Hash> DeploySmartContract(SmartContractDeployment smartContractRegister)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            
            // throw exception if not registered
            if(await smartContractMap.GetAsync(smartContractRegister.ContractHash) == null)
                throw new KeyNotFoundException("Not Registered SmartContract");
            
            var smartContractDeploymentMap =
                _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_INSTANCES);

            // calculate new account address
            var account = smartContractRegister.Caller == null ? Hash.Zero :
                Path.CalculateAccountAddress(smartContractRegister.Caller, smartContractRegister.IncrementId);
            
            // set storage
            await smartContractDeploymentMap.SetAsync(account ?? Hash.Zero, smartContractRegister.ToByteArray());

            return account;
        }
        

        /// <inheritdoc/>
        public async Task<ISmartContract> GetSmartContractAsync(Hash hash)
        {
            if (_smartContracts.ContainsKey(hash))
                return _smartContracts[hash];
            
            
            // get smart contract deployment info
            var smartContractDeploymentMap =
                _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_INSTANCES);
            var deploymentData = await smartContractDeploymentMap.GetAsync(hash);
            if (deploymentData == null)
            {
                throw new KeyNotFoundException("Not Deployed SmartContract");
            }
            var deployment = SmartContractDeployment.Parser.ParseFrom(deploymentData);
            
            // get SmartContractRegistration
            var contractHash = deployment.ContractHash;
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            var regData = await smartContractMap.GetAsync(contractHash);
            if (regData == null)
                throw new KeyNotFoundException("Not Registered SmartContract");
            var reg = SmartContractRegistration.Parser.ParseFrom(regData);

            // get runnner
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            
            // get account dataprovider
            var adp = _worldStateManager.GetAccountDataProvider(hash);
            // run smartcontract instance info and return smartcontract
            var smartContract = await runner.RunAsync(reg, deployment, adp);
            
            // cache
            _smartContracts[hash] = smartContract;

            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;
        }

        public Hash GetHash()
        {
            return Hash.Zero;
        }

        /*/// <summary>
        /// deploy a contract account
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="smartContractRegistration"></param>
        /// <returns></returns>
        public Task<IAccount> DeployAccount(Hash caller, SmartContractRegistration smartContractRegistration)
        {
            // create new account for the contract
            var calllerContext =
                _accountContextService.GetAccountDataContext(caller, _accountDataProvider.Context.ChainId);
            throw new NotImplementedException();
            //var hash = new Hash(calllerContext.CalculateHashWith(smartContractRegistration.Bytes));
            //_accountContextService.GetAccountDataContext(hash, _accountDataProvider.Context.ChainId);
            //return Task.FromResult((IAccount) new Account(hash));
        }*/
    }
}