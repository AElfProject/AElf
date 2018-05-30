using System;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private delegate void SetSmartContractContextHandler(ISmartContractContext contractContext);
        private delegate void SetTransactionContextHandler(ITransactionContext transactionContext);
        private SetSmartContractContextHandler _setSmartContractContextHandler;
        private SetTransactionContextHandler _setTransactionContextHandler;
        private ISmartContract _smartContract;

        public Executive SetApi(Type ApiType)
        {
            var scc = ApiType.GetMethod("SetSmartContractContext", BindingFlags.Public | BindingFlags.Static);
            var stc = ApiType.GetMethod("SetTransactionContext", BindingFlags.Public | BindingFlags.Static);
            var scch = Delegate.CreateDelegate(typeof(SetSmartContractContextHandler), scc);
            var stch = Delegate.CreateDelegate(typeof(SetTransactionContextHandler), stc);

            if (scch == null || stch == null)
            {
                throw new InvalidOperationException("Input is not a valid Api type");
            }

            _setSmartContractContextHandler = (SetSmartContractContextHandler)scch;
            _setTransactionContextHandler = (SetTransactionContextHandler)stch;

            return this;
        }

        public Executive SetSmartContract(ISmartContract smartContract)
        {
            _smartContract = smartContract;
            return this;
        }

        public IExecutive SetSmartContractContext(ISmartContractContext smartContractContext)
        {
            if (_setSmartContractContextHandler == null)
            {
                throw new InvalidOperationException("Api type is not set yet.");
            }
            _setSmartContractContextHandler(smartContractContext);
            return this;
        }

        public IExecutive SetTransactionContext(ITransactionContext transactionContext)
        {
            if (_setTransactionContextHandler == null)
            {
                throw new InvalidOperationException("Api type is not set yet.");
            }
            _setTransactionContextHandler(transactionContext);
            return this;
        }

        public async Task Apply()
        {
            await _smartContract.InvokeAsync();
        }
    }
}
