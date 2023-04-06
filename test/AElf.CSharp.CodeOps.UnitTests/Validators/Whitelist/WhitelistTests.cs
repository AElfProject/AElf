using System.Reflection;
using Mono.Cecil;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class WhitelistTests
    {
        [Fact]
        public void Assembly_ShouldAddAssemblyToWhitelist()
        {
            var whitelist = new Whitelist();
            var assembly = typeof(WhitelistTests).GetTypeInfo().Assembly;
            var assemblyNameReference = ConvertToAssemblyNameReference(assembly.GetName());
            
            whitelist.Assembly(assembly, Trust.Full);

            Assert.True(whitelist.ContainsAssemblyNameReference(assemblyNameReference));
            Assert.True(whitelist.CheckAssemblyFullyTrusted(assemblyNameReference));
        }

        [Fact]
        public void Namespace_ShouldAddNamespaceToWhitelist()
        {
            var whitelist = new Whitelist();
            string namespaceName = "AElf.CSharp.CodeOps.Validators.Whitelist";
            
            whitelist.Namespace(namespaceName, Permission.Allowed);

            Assert.True(whitelist.NameSpaces.ContainsKey(namespaceName));
        }

        [Fact]
        public void ContainsWildcardMatchedNamespaceRule_ShouldReturnTrueForMatchingNamespace()
        {
            var whitelist = new Whitelist();
            string wildcardNamespace = "AElf.CSharp.CodeOps.*";
            string matchingNamespace = "AElf.CSharp.CodeOps.Validators";
            
            whitelist.Namespace(wildcardNamespace, Permission.Allowed);

            Assert.True(whitelist.ContainsWildcardMatchedNamespaceRule(matchingNamespace));
        }

        [Fact]
        public void ContainsWildcardMatchedNamespaceRule_ShouldReturnFalseForNonMatchingNamespace()
        {
            var whitelist = new Whitelist();
            string wildcardNamespace = "AElf.CSharp.CodeOps.*";
            string nonMatchingNamespace = "AElf.CSharp.NonMatching";
            
            whitelist.Namespace(wildcardNamespace, Permission.Allowed);

            Assert.False(whitelist.ContainsWildcardMatchedNamespaceRule(nonMatchingNamespace));
        }

        private AssemblyNameReference ConvertToAssemblyNameReference(AssemblyName assemblyName)
        {
            return new AssemblyNameReference(assemblyName.Name, assemblyName.Version);
        }
    }
    