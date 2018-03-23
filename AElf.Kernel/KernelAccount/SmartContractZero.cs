using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractZero : ISmartContractZero
    {
        private const string SMART_CONTRACT_MAP_KEY = "SmartContractMap";

        private IAccountDataProvider _accountDataProvider;

        private readonly IDictionary<IHash, ISmartContract> _smartContracts =
            new Dictionary<IHash, ISmartContract>();


        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;

        private IWorldStateManager _worldStateManager;

        private IAccountManager _accountManager;

        private ISerializer<SmartContractRegistration> _serializer;

        public SmartContractZero(ISmartContractRunnerFactory smartContractRunnerFactory,
            IWorldStateManager worldStateManager, IAccountManager accountManager,
            ISerializer<SmartContractRegistration> serializer)
        {
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateManager = worldStateManager;
            _accountManager = accountManager;
            _serializer = serializer;
        }

        public async Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(IHash caller, string methodname, params object[] objs)
        {
            var type = typeof(SmartContractZero);
            var member = type.GetMethod(methodname);

            await (Task) member.Invoke(this, objs);
        }

        // Hard coded method in the kernel
        public async Task RegisterSmartContract(SmartContractRegistration reg)
        {
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            await smartContractMap.SetAsync(
                reg.Hash, _serializer.Serialize(reg)
            );
        }

        public async Task<ISmartContract> GetSmartContractAsync(Hash hash)
        {
            if (_smartContracts.ContainsKey(hash))
                return _smartContracts[hash];
            var smartContractMap = _accountDataProvider.GetDataProvider().GetDataProvider(SMART_CONTRACT_MAP_KEY);
            var obj = await smartContractMap.GetAsync(hash);

            var reg = _serializer.Deserialize(obj);

            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);

            var smartContract = await runner.RunAsync(reg);

            var acc = _accountManager.GetAccountByHash(new Hash(reg.Hash.Value));

            var dp = _worldStateManager.GetAccountDataProvider(_accountDataProvider.Context.ChainId, acc.GetAddress());

            await smartContract.InititalizeAsync(dp);

            _smartContracts[hash] = smartContract;

            //TODO: _smartContracts should be implemented as a memory cache with expired.

            return smartContract;
        }

        public IHash GetHash()
        {
            return Hash.Zero;
        }
    }

    public interface ISmartContractRunner
    {
        Task<ISmartContract> RunAsync(SmartContractRegistration reg);
    }
}