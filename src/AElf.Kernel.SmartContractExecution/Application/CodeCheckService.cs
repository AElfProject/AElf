using System.Threading.Tasks;
using AElf.CSharp.CodeOps;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeCheckService : ICodeCheckService, ISingletonDependency
    {
        private readonly ContractAuditor _contractAuditor = new ContractAuditor(null, null);
        
        public ILogger<CodeCheckService> Logger { get; set; }
        
        private volatile bool _isEnabled;


        public void Enable()
        {
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
        }

        public Task<bool> PerformCodeCheckAsync(byte[] code)
        {
            if (!_isEnabled)
                return Task.FromResult(false);

            Logger.LogTrace("Start code check.");
            
            try
            {
                // Check contract code
                _contractAuditor.Audit(code, true);
                return Task.FromResult(true);
            }
            catch (InvalidCodeException e)
            {
                // May do something else to indicate that the contract has an issue
                Logger.LogWarning(e.Message);
            }

            Logger.LogTrace("Finish code check.");

            return Task.FromResult(false);
        }
    }
}