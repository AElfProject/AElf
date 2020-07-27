using System.Threading.Tasks;
using AElf.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface IEvilOptionsService : IApplicationService
    {
        Task<EvilTriggerOptions> SetRepackagedTransactionAsync(bool repackagedTransaction);
        Task<EvilTriggerOptions> GetEvilTriggerOptionsAsync();
    }

    [ControllerName("EvilTrigger")]
    public class EvilOptionsService : IEvilOptionsService
    {
        private readonly EvilTriggerOptions _options;
        
        public ILogger<EvilOptionsService> Logger { get; set; }


        public EvilOptionsService(IOptionsMonitor<EvilTriggerOptions> optionsMonitor)
        {
            _options = optionsMonitor.CurrentValue;
        }


        public Task<EvilTriggerOptions> SetRepackagedTransactionAsync(bool repackagedTransaction)
        {
            _options.RepackagedTransaction = repackagedTransaction;
            Logger.LogDebug($"Evil RepackagedTransaction is {repackagedTransaction}");
            return Task.FromResult(_options);
        }

        public Task<EvilTriggerOptions> GetEvilTriggerOptionsAsync()
        {
            return Task.FromResult(_options);
        }
    }
}