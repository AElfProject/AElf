using System.Collections.Generic;
using Org.BouncyCastle.Security;

namespace AElf.Kernel.KernelAccount
{
    public class SmartContractRunnerFactory : ISmartContractRunnerFactory
    {
        private readonly Dictionary<int, ISmartContractRunner> _runners = new Dictionary<int, ISmartContractRunner>();
        public ISmartContractRunner GetRunner(int category)
        {
            if (_runners.TryGetValue(category, out var runner))
            {
                return runner;
            }

            switch (category)
            {
                case 1 :
                    runner = new CSharpSmartContractRunner();
                    _runners[category] = runner;
                    return runner;
            }

            throw new InvalidParameterException("Invalid Category for smart contract");
        }
    }
}