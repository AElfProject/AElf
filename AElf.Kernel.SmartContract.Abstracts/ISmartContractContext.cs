using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractContext
    {
        Address ContractAddress { get; }
    }
}