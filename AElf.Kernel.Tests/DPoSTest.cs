using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using AElf.Kernel.Consensus;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    // ReSharper disable once InconsistentNaming
    public class DPoSTest
    {
        private readonly DPoS _dPos;

        private readonly Assembly _dPoSContractAssembly;

        private readonly Type _processType;

        private readonly object _process;

        public DPoSTest(DPoS dPos)
        {
            _dPos = dPos;
            _dPoSContractAssembly = Assembly.Load(_dPos.ContractCode);
            _processType = _dPoSContractAssembly.GetType("AElf.Contracts.DPoS.Process");
            _process = Activator.CreateInstance(_processType);
        }

    }
}