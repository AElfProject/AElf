using Microsoft.Analytics.Interfaces;
using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AElf.WebApp.Application.Chain
{
    public class BlockChainAppServiceInitInput
    {
        public IBlockchainService blockchainService { get; set; }
        public ISmartContractAddressService smartContractAddressService { get; set; }
        public ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService { get; set; }
        public ITransactionManager transactionManager { get; set; }
        public ITransactionResultQueryService transactionResultQueryService { get; set; }
        public IBlockExtraDataService blockExtraDataService { get; set; }
        public ITxHub txHub { get; set; }
        public IBlockchainStateManager blockchainStateManager { get; set; }
        public ITaskQueueManager taskQueueManager { get; set; }
    }
}