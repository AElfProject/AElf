using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using Path = AElf.Kernel.Path;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class CSharpAssemblyLoadContextTest
    {

        private string _apiDllDirectory = "../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/";
        private string _codePath = "../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/AElf.Runtime.CSharp.Tests.TestContract.dll";

        private CSharpAssemblyLoadContext _loadContext;
        public CSharpAssemblyLoadContextTest()
        {
            _loadContext = new CSharpAssemblyLoadContext(System.IO.Path.GetFullPath(_apiDllDirectory), AppDomain.CurrentDomain.GetAssemblies());
        }

        [Fact]
        public void Test()
        {
            Assembly assembly = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(_codePath)))
            {
                assembly = _loadContext.LoadFromStream(file);
            }
            var type = assembly.GetTypes().FirstOrDefault(x => x.BaseType.Name.EndsWith("CSharpSmartContract"));
            Assert.NotNull(type);
            Assert.NotNull(_loadContext.Sdk);
        }
    }
}
