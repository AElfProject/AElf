using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public class SmartContractContext : ISmartContractContext
    {
        public Address ContractAddress { get; set; }
        
    }
}