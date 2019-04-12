using System.IO;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public class AssebmlyCheckerTests: CSharpRuntimeTestBase
    {
        private AssemblyChecker _checker;
        public AssebmlyCheckerTests()
        {
            _checker = new AssemblyChecker(null, null);
        }

        [Fact]
        public void CodeCheck_WithoutPrivilege()
        {
            var code = ReadCode(typeof(TestContract.TestContract).Assembly.Location);
            Should.NotThrow(()=>_checker.CodeCheck(code, false));
        }

        [Fact]
        public void CodeCheck_WithPrivilege()
        {
            var code = ReadCode(typeof(TestContract.TestContract).Assembly.Location);
            Should.NotThrow(()=>_checker.CodeCheck(code, true));
        }

        [Fact]
        public void CodeCheck_WithBlackList()
        {
            _checker = new AssemblyChecker(new []{"Google.Protobuf"}, null);
            var code = ReadCode(typeof(TestContract.TestContract).Assembly.Location);
            Should.Throw<InvalidCodeException>(()=>_checker.CodeCheck(code, true));
        }

        private byte[] ReadCode(string path)
        {
            if(File.Exists(path))
                return File.ReadAllBytes(path);
            return null;
        }
    }
}