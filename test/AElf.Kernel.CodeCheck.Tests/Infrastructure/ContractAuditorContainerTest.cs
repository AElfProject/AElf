using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests
{
    public partial class CodeCheckTest
    {
        [Fact]
        public void TryGetContractAuditor_Test()
        {
            _contractAuditorContainer.TryGetContractAuditor(CodeCheckConstant.SuccessAudit, out var successAuditor);
            successAuditor.GetType().GetTypeInfo().Name.ShouldBe(nameof(CustomizeAlwaysSuccessContractAuditor));
            
            _contractAuditorContainer.TryGetContractAuditor(CodeCheckConstant.FailAudit, out var failAuditor);
            failAuditor.GetType().GetTypeInfo().Name.ShouldBe(nameof(CustomizeAlwaysFailContractAuditor));
        }
        
        [Fact]
        public void ContractAuditorContainer_Test()
        {
            var auditor = new CustomizeAlwaysFailContractAuditor();
            var auditors = new List<IContractAuditor>{auditor};
            var contractAuditorContainer = new ContractAuditorContainer(auditors);
            contractAuditorContainer.TryGetContractAuditor(auditor.Category, out _).ShouldBeTrue();
        }
        
        [Fact]
        public async Task GetRequiredAcsInContractsAsync_Test()
        {
            var requireAcsConfiguration = await _requiredAcsProvider.GetRequiredAcsInContractsAsync(null, 0);
            requireAcsConfiguration.RequireAll.ShouldBe(CodeCheckConstant.IsRequireAllAcs);
            requireAcsConfiguration.AcsList.Count.ShouldBe(2);
            requireAcsConfiguration.AcsList.Contains(CodeCheckConstant.Acs1).ShouldBeTrue();
            requireAcsConfiguration.AcsList.Contains(CodeCheckConstant.Acs2).ShouldBeTrue();
        }
    }
}