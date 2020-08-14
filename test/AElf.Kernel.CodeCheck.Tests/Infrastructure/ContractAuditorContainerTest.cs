using System.Reflection;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests
{
    public partial class CodeCheckTest
    {
        [Fact]
        public async Task TryGetContractAuditor_Test()
        {
            _contractAuditorContainer.TryGetContractAuditor(CodeCheckConstant.SuccessAudit, out var successAuditor);
            successAuditor.GetType().GetTypeInfo().Name.ShouldBe(nameof(CustomizeAlwaysSuccessContractAuditor));
            
            _contractAuditorContainer.TryGetContractAuditor(CodeCheckConstant.FailAudit, out var failAuditor);
            failAuditor.GetType().GetTypeInfo().Name.ShouldBe(nameof(CustomizeAlwaysFailContractAuditor));
        }
    }
}