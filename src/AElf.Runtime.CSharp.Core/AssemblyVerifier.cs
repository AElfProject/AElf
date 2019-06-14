using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using AElf.CSharp.Core;
using AElf.Runtime.CSharp.Validators;
using AElf.Sdk.CSharp;
using Google.Protobuf.Reflection;
using ILVerify;

namespace AElf.Runtime.CSharp
{
    public class AssemblyVerifier : ResolverBase
    {
        private readonly AssemblyName _systemModule = new AssemblyName("netstandard");
        
        private readonly Dictionary<string, PEReader> _refAssemblies = new Dictionary<string, PEReader>(StringComparer.OrdinalIgnoreCase);

        private Verifier _verifier;

        public AssemblyVerifier()
        {
            // .NET path
            var netcorePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var netCoreReferences = new[]
            {
                "netstandard",
                "System.Collections",
                "System.Linq",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Private.CoreLib",
            };
            
            foreach (var systemReference in netCoreReferences)
            {
                AddReferenceAssemblyByName(netcorePath, systemReference);
            }

            // Google.Protobuf
            AddReferenceAssemblyByType(typeof(MessageDescriptor));
            
            // Add AElf related reference libraries
            AddReferenceAssemblyByType(typeof(ISmartContract));      // AElf.Types
            AddReferenceAssemblyByType(typeof(ContractStubBase));    // AElf.CSharp.Core
            AddReferenceAssemblyByType(typeof(CSharpSmartContract)); // AElf.Sdk.CSharp
        }

        private void AddReferenceAssemblyByType(Type type)
        {
            var assembly = type.Assembly;
            _refAssemblies.Add(assembly.GetName().Name, new PEReader(new MemoryStream(File.ReadAllBytes(assembly.Location))));
        }

        private void AddReferenceAssemblyByName(string path, string assemblyName)
        {
            _refAssemblies.Add(assemblyName, new PEReader(new MemoryStream(
                File.ReadAllBytes($"{path}/{assemblyName}.dll"))));
        }

        protected override PEReader ResolveCore(string simpleName)
        {
            return _refAssemblies.TryGetValue(simpleName, out var reader) ? reader : null;
        }
        
        public IEnumerable<ValidationResult> Verify(byte[] assemblyContext)
        {
            _verifier = new Verifier(this);
            _verifier.SetSystemModuleName(_systemModule);

            return VerifyAssembly(new PEReader(new MemoryStream(assemblyContext)));
        }

        private IEnumerable<AssemblyVerifierResult> VerifyAssembly(PEReader peReader)
        {
            var errors = new List<AssemblyVerifierResult>();
            
            errors.AddRange(VerifyMethods(peReader));
            errors.AddRange(VerifyTypes(peReader));

            return errors;
        }

        private IEnumerable<AssemblyVerifierResult> VerifyMethods(PEReader peReader)
        {
            var errors = new List<AssemblyVerifierResult>();
            var metadataReader = peReader.GetMetadataReader();
            
            foreach (var methodHandle in metadataReader.MethodDefinitions)
            {
                var methodName = GetQualifiedMethodName(metadataReader, methodHandle);
                
                var results = _verifier.Verify(peReader, methodHandle);
                errors.AddRange(results.Select(result => new AssemblyVerifierResult(result.Message)));
            }

            return errors;
        }
        
        private IEnumerable<AssemblyVerifierResult> VerifyTypes(PEReader peReader)
        {
            var errors = new List<AssemblyVerifierResult>();
            var metadataReader = peReader.GetMetadataReader();

            foreach (var typeHandle in metadataReader.TypeDefinitions)
            {
                var className = GetQualifiedClassName(metadataReader, typeHandle);

                var results = _verifier.Verify(peReader, typeHandle);
                errors.AddRange(results.Select(result => new AssemblyVerifierResult(result.Message)));
            }
            
            return errors;
        }

        private string GetQualifiedClassName(MetadataReader metadataReader, TypeDefinitionHandle typeHandle)
        {
            var typeDef = metadataReader.GetTypeDefinition(typeHandle);
            var typeName = metadataReader.GetString(typeDef.Name);

            var namespaceName = metadataReader.GetString(typeDef.Namespace);
            var assemblyName = metadataReader.GetString(metadataReader.IsAssembly ? metadataReader.GetAssemblyDefinition().Name : metadataReader.GetModuleDefinition().Name);

            var builder = new StringBuilder();
            builder.Append($"[{assemblyName}]");
            if (!string.IsNullOrEmpty(namespaceName))
                builder.Append($"{namespaceName}.");
            builder.Append($"{typeName}");

            return builder.ToString();
        }

        private string GetQualifiedMethodName(MetadataReader metadataReader, MethodDefinitionHandle methodHandle)
        {
            var methodDef = metadataReader.GetMethodDefinition(methodHandle);
            var typeDef = metadataReader.GetTypeDefinition(methodDef.GetDeclaringType());

            var methodName = metadataReader.GetString(metadataReader.GetMethodDefinition(methodHandle).Name);
            var typeName = metadataReader.GetString(typeDef.Name);
            var namespaceName = metadataReader.GetString(typeDef.Namespace);
            var assemblyName = metadataReader.GetString(metadataReader.IsAssembly ? metadataReader.GetAssemblyDefinition().Name : metadataReader.GetModuleDefinition().Name);

            var builder = new StringBuilder();
            builder.Append($"[{assemblyName}]");
            if (!string.IsNullOrEmpty(namespaceName))
                builder.Append($"{namespaceName}.");
            builder.Append($"{typeName}.{methodName}");

            return builder.ToString();
        }
    }
    
    public class AssemblyVerifierResult : ValidationResult
    {
        public AssemblyVerifierResult(string message) : base(message)
        {
        }
    }
}
