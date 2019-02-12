using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.GRPC;
using AElf.Crosschain.Exceptions;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Crosschain.Grpc.Client
{
    public class GrpcClientManager : IClientManager
    {
        private readonly Dictionary<int, GrpcSideChainBlockInfoRpcClient> _clientsToSideChains =
            new Dictionary<int, GrpcSideChainBlockInfoRpcClient>();

        private GrpcParentChainBlockInfoRpcClient _grpcParentChainBlockInfoRpcClient;
        private CertificateStore _certificateStore;
        private CancellationTokenSource _tokenSourceToSideChain;
        private CancellationTokenSource _tokenSourceToParentChain;

        /// <summary>
        /// Initialize client manager.
        /// </summary>
        /// <param name="dir"></param>
        public void Init(string dir)
        {
            _certificateStore = new CertificateStore(dir);
            _tokenSourceToSideChain = new CancellationTokenSource();
            _tokenSourceToParentChain = new CancellationTokenSource();
        }

        /// <summary>
        /// Extend interval for request after initial block synchronization.
        /// </summary>
        public void UpdateRequestInterval(int interval)
        {
            _grpcParentChainBlockInfoRpcClient?.UpdateRequestInterval(interval);
            _clientsToSideChains.AsParallel().ToList().ForEach(kv => { kv.Value.UpdateRequestInterval(interval); });
        }

        #region Create client

        public void CreateClient(IClientBase grpcClientBase)
        {
            var client = CreateGrpcClient((GrpcClientBase) grpcClientBase);
            //client = clientBasicInfo.TargetIsSideChain ? (ClientToSideChain) client : (ClientToParentChain) client;
            client.StartDuplexStreamingCall(((GrpcClientBase) grpcClientBase).TargetIsSideChain
                ? _tokenSourceToSideChain.Token
                : _tokenSourceToParentChain.Token);
        }

        /// <summary>
        /// Create a new client to parent chain 
        /// </summary>
        /// <returns>
        /// </returns>    
        private IGrpcCrossChainClient CreateGrpcClient(GrpcClientBase grpcClientBase)
        {
            var channel = CreateChannel(grpcClientBase.ToUriStr(), grpcClientBase.TargetChainId);

            if (grpcClientBase.TargetIsSideChain)
            {
                var clientToSideChain = new GrpcSideChainBlockInfoRpcClient(channel, grpcClientBase);
                _clientsToSideChains.Add(grpcClientBase.TargetChainId, clientToSideChain);
                return clientToSideChain;
            }

            _grpcParentChainBlockInfoRpcClient = new GrpcParentChainBlockInfoRpcClient(channel, grpcClientBase);
            return _grpcParentChainBlockInfoRpcClient;
        }

        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="targetChainId"></param>
        /// <returns></returns>
        /// <exception cref="CertificateException"></exception>
        private Channel CreateChannel(string uriStr, int targetChainId)
        {
            string crt = _certificateStore.GetCertificate(targetChainId.ToString());
            if (crt == null)
                throw new CertificateException("Unable to load Certificate.");
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            return channel;
        }

        #endregion


        /// <summary>
        /// Close and clear clients to side chain
        /// </summary>
        public void CloseClientsToSideChain()
        {
            _tokenSourceToSideChain?.Cancel();
            _tokenSourceToSideChain?.Dispose();

            //Todo: probably not needed
            _clientsToSideChains.Clear();
        }

        /// <summary>
        /// close and clear clients to parent chain
        /// </summary>
        public void CloseClientToParentChain()
        {
            _tokenSourceToParentChain?.Cancel();
            _tokenSourceToParentChain?.Dispose();

            //Todo: probably not needed
            _grpcParentChainBlockInfoRpcClient = null;
        }
    }
}