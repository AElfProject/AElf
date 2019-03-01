using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Sdk.CSharp.State
{
    public class FuncInfo<T>
    {
        private readonly ContractReferenceState _owner;
        private readonly string _name;

        public FuncInfo(ContractReferenceState owner, string name)
        {
            _owner = owner;
            _name = name;
        }

        internal T Call(params object[] args)
        {
            throw new NotImplementedException();
            /*
            var svc = _owner.Context.SmartContractContext.SmartContractExecutiveService;
            var transactionContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = _owner.Context.Self,
                    To = _owner.Value,
                    MethodName = _name,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
                }
            };
            int chainId = _owner.Context.SmartContractContext.ChainId;
            var chainContext = new ChainContext()
            {
                ChainId = chainId,
                BlockHash = _owner.Context.TransactionContext.PreviousBlockHash,
                BlockHeight = _owner.Context.TransactionContext.BlockHeight - 1
            };
            AsyncHelper.RunSync(async () =>
            {
                var executive = await svc.GetExecutiveAsync(chainId, chainContext, _owner.Value);
                executive.SetDataCache(_owner.Provider.Cache);
                try
                {
                    // view only, write actions need to be sent via SendInline
                    await executive.SetTransactionContext(transactionContext).Apply();
                }
                finally
                {
                    await svc.PutExecutiveAsync(chainId, _owner.Value, executive);
                }
            });
            // TODO: Maybe put readonly call trace into inlinetraces to record data access

            if (!transactionContext.Trace.IsSuccessful())
            {
                throw new ContractCallError($"Failed to call view method {_name} in contract {_owner.Value}.");
            }

            return (T) transactionContext.Trace.RetVal.Data.DeserializeToType(typeof(T));*/
        }
    }
}
