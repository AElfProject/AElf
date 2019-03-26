using System;
using System.Threading.Tasks;
using System.Timers;
using AElf.Management.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Management.Services
{
    
    //TODO: timer is not stable, we should use something like hangfire
    public class RecordService : IRecordService, ISingletonDependency
    {
        private readonly IChainService _chainService;
        private readonly ITransactionService _transactionService;
        private readonly INodeService _nodeService;
        private readonly INetworkService _networkService;
        private readonly ManagementOptions _managementOptions;
        private readonly Timer _timer;

        public ILogger<RecordService> Logger {get;set;}

        public RecordService(IChainService chainService, ITransactionService transactionService, INodeService 
        nodeService, INetworkService networkService,IOptionsSnapshot<ManagementOptions> options)
        {
            Logger= NullLogger<RecordService>.Instance;

            _chainService = chainService;
            _transactionService = transactionService;
            _nodeService = nodeService;
            _networkService = networkService;
            _managementOptions = options.Value;
            _timer = new Timer(_managementOptions.MonitoringInterval * 1000);
            _timer.Elapsed += TimerOnElapsed;
        }

        public void Start()
        {
            // Todo we should move it to monitor project,management website just receive and record
            _timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var time = DateTime.Now;
            Parallel.ForEach(_managementOptions.ServiceUrls.Keys, chainId =>
                {
                    try
                    {
                        _transactionService.RecordPoolSize(chainId, time);
                        //_nodeService.RecordPoolState(chainId, time);
                        _nodeService.RecordBlockInfo(chainId);
                        _nodeService.RecordGetCurrentChainStatus(chainId, time);
                        //_nodeService.RecordInvalidBlockCount(chainId, time);
                        //_nodeService.RecordRollBackTimes(chainId, time);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "record data error.");
                    }
                }
            );
        }
    }
}