using System;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractRunnerFactory : ISmartContractRunnerFactory
    {
        
        private CSharpSmartContractRunner CSharpSmartContractRunner { get; set; }
        public ISmartContractRunner GetRunner(int category)
        {
            if(category == 1)
                return CSharpSmartContractRunner;
            throw new NotImplementedException();
        }
    }

    
    public class CSharpSmartContractRunner : ISmartContractRunner
    {
        private readonly ISerializer<SmartContractRegistration> _serializer;

        public CSharpSmartContractRunner(ISerializer<SmartContractRegistration> serializer)
        {
            _serializer = serializer;
        }

        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg)
        {
            return await Task.FromResult(new CSharpSmartContract(_serializer, reg));
        }
    }

    public class SmartContractZeroRunner : ISmartContractRunner
    {
        private readonly ISerializer<SmartContractZero> _serializer;

        public SmartContractZeroRunner(ISerializer<SmartContractZero> serializer)
        {
            _serializer = serializer;
        }

        public Task<ISmartContract> RunAsync(SmartContractRegistration reg)
        {
            ISmartContract smartContract = _serializer.Deserialize(reg.ContractBytes.ToByteArray());
            return Task.FromResult(smartContract);
        }
    }
}