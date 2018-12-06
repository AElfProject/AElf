using System;
using System.Threading.Tasks;
using System.Timers;
using AElf.Configuration;
using AElf.Configuration.Config.Management;
using AElf.Management.Interfaces;
using NLog;

namespace AElf.Management.Services
{
    public class RecordService : IRecordService
    {
        private readonly IChainService _chainService;
        private readonly ITransactionService _transactionService;
        private readonly INodeService _nodeService;
        private readonly INetworkService _networkService;
        private readonly Timer _timer;

        private readonly ILogger _logger;

        public RecordService(IChainService chainService, ITransactionService transactionService, INodeService nodeService, INetworkService networkService)
        {
            _logger = LogManager.GetLogger("RecordService");

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
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "record data error.");
                    }
                }
            );
        }
    }
}