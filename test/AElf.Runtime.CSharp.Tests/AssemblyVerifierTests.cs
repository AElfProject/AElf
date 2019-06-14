using System.IO;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.MultiToken;
using Xunit;
using Shouldly;

namespace AElf.Runtime.CSharp
{
    public class AssemblyVerifierTests
    {
        [Fact]
        public void VerifyContract_Test()
        {
            var code = ReadCode(typeof(TokenContract).Assembly.Location);
            var verifier = new AssemblyVerifier();
            verifier.Verify(code);
        }
        
        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
        }
    }
}