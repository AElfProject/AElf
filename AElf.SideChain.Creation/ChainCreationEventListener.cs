using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AElf.ChainController;

using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using AElf.Kernel.Managers;
using SideChainInfo = AElf.Kernel.SideChainInfo;
using AElf.Configuration.Config.Chain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.SideChain.Creation
{
    
    public class ChainCreationEventListener
    {
        private HttpClient _client;
        public ILogger<ChainCreationEventListener> Logger {get;set;}
        private ITransactionResultManager TransactionResultManager { get; set; }
        private IChainCreationService ChainCreationService { get; set; }
        private LogEvent _interestedLogEvent;
        private Bloom _bloom;
        private IChainManager _chainManager;

        public ChainCreationEventListener( ITransactionResultManager transactionResultManager, 
            IChainCreationService chainCreationService, IChainManager chainManager)
        {
            Logger = NullLogger<ChainCreationEventListener>.Instance;
            TransactionResultManager = transactionResultManager;
            ChainCreationService = chainCreationService;
            _chainManager = chainManager;
            _interestedLogEvent = new LogEvent()
            {
                Address = ContractHelpers.GetGenesisBasicContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId()),
                Topics =
                {
                    ByteString.CopyFrom("SideChainCreationRequestApproved".CalculateHash())
                }
            };
            _bloom = _interestedLogEvent.GetBloom();
            InitializeClient();
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
                Logger.LogInformation($"Chain creation event: {info}");
                try
                {
                    var response = await SendChainDeploymentRequestFor(info.ChainId, chainId);
                    
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.LogError(
                            $"Sending sidechain deployment request for {info.ChainId} failed. " +
                            $"StatusCode: {response.StatusCode}."
                        );
                    }
                    else
                    {
                        Logger.LogInformation(
                            $"Successfully sent sidechain deployment request for {info.ChainId}. " +
                            $"Management API return message: {await response.Content.ReadAsStringAsync()}."
                        );
                        
                        // insert
                        //await _chainManagerBasic.AddSideChainId(info.ChainId);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Sending sidechain deployment request for {info.ChainId} failed due to exception.");
                }
            }
        }

        #region Http

        private void InitializeClient()
        {
            if (string.IsNullOrWhiteSpace(NodeConfig.Instance.DeployServicePath))
            {
                Logger.LogError("Must set the path of deploy Service");
                return;
            }

            _client = new HttpClient {BaseAddress = new Uri(NodeConfig.Instance.DeployServicePath)};
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    
        private async Task<HttpResponseMessage> SendChainDeploymentRequestFor(int sideChainId, int parentChainId)
        {
            var chainId = parentChainId.DumpBase58();
            var endpoint = NodeConfig.Instance.DeployServicePath.TrimEnd('/') + "/" + chainId;
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            var deployArg = new DeployArg();
            deployArg.SideChainId = sideChainId.DumpBase58();
            deployArg.AccountPassword = "123";
            deployArg.LauncherArg.IsConsensusInfoGenerator = true;
            deployArg.LighthouseArg.IsCluster = false;
            var content = JsonSerializer.Instance.Serialize(deployArg);
            var c = new StringContent(content);
            c.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = c;
            
            return await _client.SendAsync(request);
        }

        #endregion Http
    }
}