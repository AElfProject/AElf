using System;
using System.Threading.Tasks;
using System.Timers;
using AElf.Configuration;
using AElf.Configuration.Config.Management;
using AElf.Management.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Management.Services
{
    public class RecordService : IRecordService
    {
        private readonly IChainService _chainService;
        private readonly ITransactionService _transactionService;
        private readonly INodeService _nodeService;
        private readonly INetworkService _networkService;
        private readonly Timer _timer;

        public ILogger<RecordService> Logger {get;set;}

        public RecordService(IChainService chainService, ITransactionService transactionService, INodeService nodeService, INetworkService networkService)
        {
            Logger= NullLogger<RecordService>.Instance;

            _chainService = chainService;
            _transactionService = transactionService;
            _nodeService = nodeService;
            _networkService = networkService;
            _timer = new Timer(ManagementConfig.Instance.MonitoringInterval * 1000);
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
            Parallel.ForEach(ServiceUrlConfig.Instance.ServiceUrls.Keys, chainId =>
                {
                    try
                    {
                        _transactionService.RecordPoolSize(chainId, time);
                        _nodeService.RecordPoolState(chainId, time);
                        _nodeService.RecordBlockInfo(chainId);
                        _nodeService.RecordInvalidBlockCount(chainId, time);
                        _nodeService.RecordRollBackTimes(chainId, time);
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