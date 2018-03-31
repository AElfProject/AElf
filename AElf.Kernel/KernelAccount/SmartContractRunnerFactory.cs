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
        private ISerializer<SmartContractRegistration> _serializer;

        public CSharpSmartContractRunner(ISerializer<SmartContractRegistration> serializer)
        {
            _serializer = serializer;
        }

        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg)
        {
            return await Task.FromResult(new CSharpSmartContract(_serializer, reg));
        }
    }
}