using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using AElf.Contracts;
using Path = AElf.Kernel.Path;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class CSharpAssemblyLoadContextTest
    {

        private string _apiDllDirectory = "../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/";
        private string _codePath = "../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/AElf.Contracts.Examples.dll";

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
            assembly.GetTypes();
            //Assert.Equal("", string.Join(" ", assembly.GetTypes()[3].Name));

            Assert.NotNull(_loadContext.Sdk);

            //var sdk = assembly.GetReferencedAssemblies().FirstOrDefault(y => y.Name.StartsWith("AElf.Sdk"));
            var ApiSingleton = _loadContext.Sdk.GetTypes().FirstOrDefault(x => x.Name.EndsWith("Api"));
            Assert.NotNull(ApiSingleton);
            //Assert.Equal("", string.Join(" ", assembly.GetExportedTypes().Select(y => y.ToString())));
            Assert.Equal("", string.Join(" ", assembly.GetReferencedAssemblies().Where(y => y.Name.StartsWith("AElf.Sdk"))));
            Assert.Equal("", string.Join(" ", assembly.GetTypes()[3].Name));
            Assert.Equal("", assembly.GetTypes()[0].ToString());
            Assert.NotNull(assembly);
        }
    }
}
