using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.CodeCheck.Tests
{
    public class CustomizeAlwaysSuccessContractAuditor : IContractAuditor, ISingletonDependency
    {
        public int Category { get; } = CodeCheckConstant.SuccessAudit;
        public void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract)
        {
        }
    }
    
    public class CustomizeAlwaysFailContractAuditor : IContractAuditor, ISingletonDependency
    {
        public int Category { get; } = CodeCheckConstant.FailAudit;
        public void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract)
        {
            throw new InvalidCodeException("failed");
        }
    }
}