using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> where TContractState : ContractState
    {
        private readonly IContextInternal _context = new Context();

        public IContext Context => _context;

        public TContractState State { get; internal set; }

        public CSharpSmartContract()
        {
            State = Activator.CreateInstance<TContractState>();
        }

        public ulong GetMethodFee(string methodName)
        {
            return State.__MethodFees__[methodName];
        }

        public void SetMethodFee(string methodName, ulong fee)
        {
            State.__MethodFees__[methodName] = fee;
        }
    }
}