using System;
using System.IO;
using System.Reflection;
using AElf.TestBase;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps
{
    public class CSharpCodeOpsTestBase : AElfIntegratedTest<TestCSharpCodeOpsAElfModule>
    {
        private const string ContractDllDir = "../../../../../src/AElf.Launcher/contracts/";
        protected const string ContractPatchedDllDir = "../../../../patched/";
        
        protected byte[] ReadContractCode(Type contractType)
        {
            var location = Path.Combine(ContractDllDir, Assembly.GetAssembly(contractType).ManifestModule.Name);
            return ReadCode( location);
        }
        
        protected byte[] ReadContractCode(string moduleName)
        {
            var location = Path.Combine(ContractDllDir, moduleName);
            return ReadCode(location);
        }

        protected byte[] ReadPatchedContractCode(Type contractType)
        {
            return ReadCode(ContractPatchedDllDir + contractType.Module + ".patched");
        }

        protected ModuleDefinition GetModule(Type contractType)
        {
            var code = ReadContractCode(contractType);
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            return modDef;
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path)
                ? File.ReadAllBytes(path)
                : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
    }
}