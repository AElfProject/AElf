using System;
using System.Collections.Generic;
using System.Timers;
using AElf.Management.Interfaces;
using AElf.Management.Models;

namespace AElf.Management.Services
{
    public class RecrodService:IRecrodService
    {
        private readonly IChainService _chainService;
        private readonly ITransactionService _transactionService;
        private readonly INodeService _nodeService;
        private readonly INetworkService _networkService;
        
        public RecrodService(IChainService chainService,ITransactionService transactionService,INodeService nodeService,INetworkService networkService)
        {
            _chainService = chainService;
            _transactionService = transactionService;
            _nodeService = nodeService;
            _networkService = networkService;
        }

        public void Start()
        {
            var timer = new Timer(10000);
            timer.Elapsed+= TimerOnElapsed;
            timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var chains = new List<ChainResult>(); // _chainService.GetAllChains();
            chains.Add(new ChainResult{ChainId = "0x2491b3fb14d2ddac790fc18c161166226f04"});
            var time = DateTime.Now;
            foreach (var chain in chains)
            {
                var chainId = chain.ChainId;

                var txPoolSize = _transactionService.GetPoolSize(chainId);
                _transactionService.RecordPoolSize(chainId, time, txPoolSize);

                var isAlive = _nodeService.IsAlive(chainId);
                var isForked = _nodeService.IsForked(chainId);
                _nodeService.RecordPoolState(chainId,time,isAlive,isForked);

                var networkState = _networkService.GetPoolState(chainId);
                _networkService.RecordPoolState(chainId, time, networkState.RequestPoolSize, networkState.ReceivePoolSize);
            }
        }
    }
}