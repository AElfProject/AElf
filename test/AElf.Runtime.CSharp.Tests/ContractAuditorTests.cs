using System.IO;
using System.Linq;
using Shouldly;
using Xunit;
using Mono.Cecil;

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
            
            result.ShouldBeEmpty();
        }
        
        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }

    class DummyContract
    {
        double TestDouble()
        {
            return (double) 1.0;
        }

        float TestFloat()
        {
            return (float) 1.0;
        }
    }
}