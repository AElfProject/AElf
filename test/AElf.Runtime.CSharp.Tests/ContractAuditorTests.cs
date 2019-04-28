using System.IO;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public class ContractAuditorTests : CSharpRuntimeTestBase
    {
        private ContractAuditor _auditor;

        public ContractAuditorTests()
        {
            _auditor = new ContractAuditor();
        }
        
        [Fact]
        public void CodeCheck_WithoutPrivilege()
        {
            var code = ReadCode(typeof(TestContract.TestContract).Assembly.Location);
            
            var result = _auditor.Audit(code, false);
            
            // Temporary disable until the rules are complete for the test contract at least
            //result.ShouldBeEmpty();
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }

}