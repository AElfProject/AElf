using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class AssemblyListerWorker : PeriodicBackgroundWorkerBase
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public AssemblyListerWorker(AbpTimer timer, ISmartContractExecutiveService smartContractExecutiveService) : base(timer)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
            Timer.Period = 10_000;
        }
        
        protected override void DoWork()
        {
            AsyncHelper.RunSync(ProcessDownloadJobAsync);
        }

        internal async Task ProcessDownloadJobAsync()
        {
            Logger.LogDebug("Listing assemblies.");
            
            _smartContractExecutiveService.PrintUnloadedAssemblies();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();

            try
            {
//                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("AElf.Contracts.MultiToken")))
//                {
//                    Logger.LogDebug("AElf assembly: " + ass.FullName);
//                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error checking assemblies.");
            }
            
            Logger.LogDebug("Finished.");
        }
    }
}