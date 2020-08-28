using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.CodeCheck.Application
{
    public class CodeCheckService : ICodeCheckService, ITransientDependency
    {
        private readonly IRequiredAcsProvider _requiredAcsProvider;
        private readonly IContractAuditorContainer _contractAuditorContainer;
        private readonly CodeCheckOptions _codeCheckOptions;

        public ILogger<CodeCheckService> Logger { get; set; }


        public CodeCheckService(IRequiredAcsProvider requiredAcsProvider,
            IContractAuditorContainer contractAuditorContainer,
            IOptionsMonitor<CodeCheckOptions> codeCheckOptionsMonitor)
        {
            _requiredAcsProvider = requiredAcsProvider;
            _contractAuditorContainer = contractAuditorContainer;
            _codeCheckOptions = codeCheckOptionsMonitor.CurrentValue;
        }

        public async Task<bool> PerformCodeCheckAsync(byte[] code, Hash blockHash, long blockHeight, int category, bool isSystemContract)
        {
            if (!_codeCheckOptions.CodeCheckEnabled)
                return false;

            var requiredAcs =
                await _requiredAcsProvider.GetRequiredAcsInContractsAsync(blockHash, blockHeight);
            try
            {
                // Check contract code
                Logger.LogTrace("Start code check.");
                if (!_contractAuditorContainer.TryGetContractAuditor(category, out var contractAuditor))
                {
                    Logger.LogWarning($"Unrecognized contract category: {category}");
                    return false;
                }
                
                contractAuditor.Audit(code, requiredAcs, isSystemContract);
                Logger.LogTrace("Finish code check.");
                return true;
            }
            catch (InvalidCodeException e)
            {
                // May do something else to indicate that the contract has an issue
                Logger.LogWarning(e.Message);
            }

            return false;
        }
    }
}