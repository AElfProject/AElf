using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using AElf.Kernel.Managers;
using Newtonsoft.Json.Linq;
using NLog;
using SideChainInfo = AElf.Contracts.SideChain.SideChainInfo;

namespace AElf.SideChain.Creation
{
    [LoggerName(nameof(ChainCreationEventListener))]
    public class ChainCreationEventListener
    {
        private HttpClient _client;
        private ILogger _logger;
        private ITransactionResultManager TransactionResultManager { get; set; }
        private IChainCreationService ChainCreationService { get; set; }
        private LogEvent _interestedLogEvent;
        private Bloom _bloom;
        private ISideChainManager _sideChainManager;

        public ChainCreationEventListener(ILogger logger, ITransactionResultManager transactionResultManager, 
            IChainCreationService chainCreationService, ISideChainManager sideChainManager)
        {
            _logger = logger;
            TransactionResultManager = transactionResultManager;
            ChainCreationService = chainCreationService;
            _sideChainManager = sideChainManager;
            _interestedLogEvent = new LogEvent()
            {
                Address = GetGenesisContractHash(),
                Topics =
                {
                    ByteString.CopyFrom("SideChainCreationRequestApproved".CalculateHash())
                }
            };
            _bloom = _interestedLogEvent.GetBloom();
            InitializeClient();
        }

        private Hash GetGenesisContractHash()
        {
            return ChainCreationService.GenesisContractHash(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), SmartContractType.BasicContractZero);
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

            var chainId = block.Header.ChainId;
            var infos = new List<SideChainInfo>();
            foreach (var txId in block.Body.Transactions)
            {
                var res = await TransactionResultManager.GetTransactionResultAsync(txId);
                infos.AddRange(GetInterestedEvent(res));
            }

            foreach (var info in infos)
            {
                _logger?.Info("Chain creation event: " + info);
                try
                {
                    var response = await SendChainDeploymentRequestFor(info.ChainId, chainId);
                    
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger?.Error(
                            $"Sending sidechain deployment request for {info.ChainId} failed. " +
                            $"StatusCode: {response.StatusCode}"
                        );
                    }
                    else
                    {
                        _logger?.Info(
                            $"Successfully sent sidechain deployment request for {info.ChainId}. " +
                            "Management API return message: " + await response.Content.ReadAsStringAsync()
                        );
                        
                        // insert 
                        await _sideChainManager.AddSideChain(info.ChainId);
                    }
                }
                catch (Exception e)
                {
                    _logger?.Error(e, $"Sending sidechain deployment request for {info.ChainId} failed due to exception.");
                }
            }
        }

        #region Http

        private void InitializeClient()
        {
            _client = new HttpClient {BaseAddress = new Uri(ManagementConfig.Instance.Url)};
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    
        private async Task<HttpResponseMessage> SendChainDeploymentRequestFor(Hash sideChainId, Hash parentChainId)
        {
            var endpoint = ManagementConfig.Instance.SideChainServicePath.TrimEnd('/') + "/" + sideChainId.ToHex();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            var content = new JObject()
            {
                ["MainChainAccount"] = ManagementConfig.Instance.NodeAccount,
                ["AccountPassword"] = ManagementConfig.Instance.NodeAccountPassword
            }.ToString();
            var c = new StringContent(content);
            c.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = c;
            return await _client.SendAsync(request);
        }

        #endregion Http
    }
}