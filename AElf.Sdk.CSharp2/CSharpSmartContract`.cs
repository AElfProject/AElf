using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class CSharpSmartContract<TContractState> : CSharpSmartContractAbstract
        where TContractState : ContractState
    {
        internal override void SetStateManager(IStateManager stateManager)
        {
            State.Manager = stateManager;
        }

        internal override void SetContractAddress(Address address)
        {
            var path = new StatePath();
            path.Path.Add(ByteString.CopyFromUtf8(address.GetFormatted()));
            State.Path = path;
        }

        internal override void Cleanup()
        {
            State.Clear();
        }

        public IContext Context { get; internal set; }
        public TContractState State { get; internal set; }

        public CSharpSmartContract()
        {
            State = Activator.CreateInstance<TContractState>();
        }
    }
}