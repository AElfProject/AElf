using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionGenerator
    {
        Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash);
    }

    public interface ISystemTransactionRecognizer
    {
        bool IsSystemTransaction(Transaction transaction);
    }

    public abstract class SystemTransactionRecognizerBase : ISystemTransactionRecognizer
    {
        public abstract bool IsSystemTransaction(Transaction transaction);

        protected bool CheckSystemContractAddress(Address addressFromTransaction, Address contractAddress)
        {
            return addressFromTransaction == contractAddress;
        }

        protected bool CheckSystemContractMethod(string methodNameFromTransaction, params string[] systemContractMethods)
        {
            return systemContractMethods.Any(methodName => methodName == methodNameFromTransaction);
        }
    }
}