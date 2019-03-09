using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContractBridge;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> where TContractState : ContractState
    {
        private ISmartContractBridgeContext _context;

        public ISmartContractBridgeContext Context
        {
            get => _context;
            private set
            {
                _context = value;
                SetContractAddress(_context.Sender);
            }
        }

        public TContractState State { get; internal set; }

        public CSharpSmartContract()
        {
            State = Activator.CreateInstance<TContractState>();
        }
    }
}