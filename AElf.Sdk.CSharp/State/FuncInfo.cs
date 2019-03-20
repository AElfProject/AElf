using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    //TODO: Add FuncInfo test case [Case]
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
            return _owner.Context.Call<T>(_owner.Provider.Cache, _owner.Value, _name,
                ByteString.CopyFrom(ParamsPacker.Pack(args)));
        }
    }
}