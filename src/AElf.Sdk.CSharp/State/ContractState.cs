using System.IO;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Sdk.CSharp.State
{
    public class ContractState : StructuredState
    {
        public SingletonState<Address> Owner { get; set; }
    }
}