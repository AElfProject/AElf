using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public abstract class SmartContract : ISmartContract
    {
        private IAccountDataProvider _accountDataProvider;
        private ISerializer<SmartContractRegistration> _serializer;

        protected SmartContract(ISerializer<SmartContractRegistration> serializer)
        {
            _serializer = serializer;
        }

        public async Task InititalizeAsync(IAccountDataProvider dataProvider)
        {
            _accountDataProvider = dataProvider;
            await Task.CompletedTask;
        }

        public async Task InvokeAsync(IHash caller, string methodname, params object[] objs)
        {
            throw new NotImplementedException();
            /*
            // get smartContractRegistration by accountDataProvider 
            var smartContractRegistrationBytes = await _accountDataProvider.GetDataProvider()
                .GetDataProvider("SmartContract")
                .GetAsync(new Hash(_accountDataProvider.CalculateHashWith("SmartContract")));
            var smartContractRegistration = _serializer.Deserialize(smartContractRegistrationBytes);
            // load assembly with bytes
            var assembly = Assembly.Load(smartContractRegistration.Bytes);
            var type = assembly.GetTypes().ElementAt(0);
            var method = type.GetMethod(methodname);
            
            if (type.GetConstructors().Length == 0)
            {
                // if contract is static, first param will be ignore
                await (Task) method.Invoke(null, objs);
            }*/
        }

        public abstract IHash GetHash();
    }
}