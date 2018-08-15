using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Configuration;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using AElf.Contracts.Genesis;
using AElf.Kernel.Managers;
using Newtonsoft.Json.Linq;
using NLog;

namespace AElf.SideChain.Creation
{
    [LoggerName(nameof(ChainCreationEventListener))]
    public class ChainCreationEventListener
    {
        private ILogger _logger;
        private ITransactionResultManager TransactionResultManager { get; set; }
        private IChainCreationService ChainCreationService { get; set; }
        private INodeConfig NodeConfig { get; set; }
        private IManagementConfig ManagementConfig { get; set; }
        private LogEvent _interestedLogEvent;
        private Bloom _bloom;
        private HttpRequestor _httpRequestor;

        public ChainCreationEventListener(ILogger logger, ITransactionResultManager transactionResultManager,
            IChainCreationService chainCreationService, INodeConfig nodeConfig, IManagementConfig managementConfig)
        {
            _logger = logger;
            TransactionResultManager = transactionResultManager;
            ChainCreationService = chainCreationService;
            NodeConfig = nodeConfig;
            ManagementConfig = managementConfig;
            _interestedLogEvent = new LogEvent()
            {
                Address = GetGenesisContractHash(),
                Topics =
                {
                    ByteString.CopyFrom("SideChainCreationRequestApproved".CalculateHash())
                }
            };
            _bloom = _interestedLogEvent.GetBloom();
            _httpRequestor = new HttpRequestor(ManagementConfig.Url);
        }

        private Hash GetGenesisContractHash()
        {
            return ChainCreationService.GenesisContractHash(NodeConfig.ChainId, SmartContractType.BasicContractZero);
        }

        private List<SideChainInfo> GetInterestedEvent(TransactionResult result)
        {
            var res = new List<SideChainInfo>();
            foreach (var le in result.Logs)
            {
                if (le.Topics.Count < _interestedLogEvent.Topics.Count)
                {
                    continue;
                }

                for (var i = 0; i < _interestedLogEvent.Topics.Count; i++)
                {
                    if (le.Topics[i] != _interestedLogEvent.Topics[i])
                    {
                        break;
                    }
                }

                res.Add(
                    (SideChainInfo) ParamsPacker.Unpack(le.Data.ToByteArray(),
                        new System.Type[] {typeof(SideChainInfo)})[0]
                );
            }

            return res;
        }

        public async Task OnBlockAppended(IBlock block)
        {
            // TODO: OnBlockIrreversible instead
            if (!_bloom.IsIn(new Bloom(block.Header.Bloom.ToByteArray())))
            {
                return;
            }

            var infos = new List<SideChainInfo>();
            foreach (var txId in block.Body.Transactions)
            {
                var res = await TransactionResultManager.GetTransactionResultAsync(txId);
                infos.AddRange(GetInterestedEvent(res));
            }

            foreach (var info in infos)
            {
                _logger?.Info("Chain creation event: " + info);
                var res = _httpRequestor.DoRequest(
                    info.ChainId.ToHex(),
                    new JObject()
                    {
                        ["MainChainAccount"] = ManagementConfig.NodeAccount,
                        ["AccountPassword"] = ManagementConfig.NodeAccountPassword
                    }.ToString());
                _logger?.Info("Management API return message: " + res);
            }
        }
    }
}