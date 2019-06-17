using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using AElf.Runtime.CSharp.Validators;
using ILVerify;

namespace AElf.Runtime.CSharp
{
    public class ILVerifier : ResolverBase
    {
        private readonly AssemblyName _systemModule = new AssemblyName("netstandard");
        
        private readonly Dictionary<string, PEReader> _refAssemblies = new Dictionary<string, PEReader>(StringComparer.OrdinalIgnoreCase);

        private Verifier _verifier;

        public ILVerifier(IEnumerable<string> assemblyReferences)
        {
            foreach (var assemblyReference in assemblyReferences)
            {
                _refAssemblies.Add(assemblyReference, new PEReader(new MemoryStream(
                    File.ReadAllBytes(Assembly.Load(assemblyReference).Location))));
            }
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

        private IEnumerable<ILVerifierResult> VerifyAssembly(PEReader peReader)
        {
            var errors = new List<ILVerifierResult>();
            
            errors.AddRange(VerifyMethods(peReader));
            errors.AddRange(VerifyTypes(peReader));

            return errors;
        }

        private IEnumerable<ILVerifierResult> VerifyMethods(PEReader peReader)
        {
            var errors = new List<ILVerifierResult>();
            var metadataReader = peReader.GetMetadataReader();
            
            foreach (var methodHandle in metadataReader.MethodDefinitions)
            {
                var methodName = GetQualifiedMethodName(metadataReader, methodHandle);
                
                var results = _verifier.Verify(peReader, methodHandle);
                errors.AddRange(results.Select(result => new ILVerifierResult(result.Message)));
            }

            return errors;
        }
        
        private IEnumerable<ILVerifierResult> VerifyTypes(PEReader peReader)
        {
            var errors = new List<ILVerifierResult>();
            var metadataReader = peReader.GetMetadataReader();

            foreach (var typeHandle in metadataReader.TypeDefinitions)
            {
                var className = GetQualifiedClassName(metadataReader, typeHandle);

                var results = _verifier.Verify(peReader, typeHandle);
                errors.AddRange(results.Select(result => new ILVerifierResult(result.Message)));
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
    
    public class ILVerifierResult : ValidationResult
    {
        public ILVerifierResult(string message) : base(message)
        {
        }
    }
}
