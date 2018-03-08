using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class ChainContext : IChainContext
    {
        public ISmartContractZero SmartContractZero { get; private set; }

        public IHash<IChain> ChainId { get; private set; }

        public bool InitializeChain(IChain chain, AccountZero accountZero, ITransaction transaction)
        {
            ChainId = chain.Id;
            SmartContractZero = accountZero.SmartContractZero;
            return DeployContractInAccountZero(transaction).Result;
        }
        
        /// <summary>
        /// deploy contract for AccountZero init
        /// </summary>
        private Task<bool> DeployContractInAccountZero(ITransaction transaction)
        {
            var objs = transaction.Params;
            
            if (objs.Length < 3)
            {
                return Task.FromResult(false);
            }

            var contractName = (string)objs[0];
            var category = (int)objs[1];
            var data = (byte[])objs[2];
            
            // create registration
            var smartContractRegistration = new SmartContractRegistration
            {
                Name = contractName,
                Category =  category,
                Bytes = data,
                // temporary calculating method for new address
                Hash = new Hash<IAccount>(Hash<IAccount>.Zero.CalculateHashWith(contractName))
            };

            // wait register processing done
            SmartContractZero.RegisterSmartContract(smartContractRegistration).Wait();

            return Task.FromResult(true);
        }
    }
}