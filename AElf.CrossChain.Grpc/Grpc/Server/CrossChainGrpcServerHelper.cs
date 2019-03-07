using System.Collections.Generic;
using AElf.CrossChain.Grpc.Exceptions;
using AElf.Cryptography.Certificate;
using Grpc.Core;

namespace AElf.CrossChain.Grpc.Server
{
    public class CrossChainGrpcServerHelper
    {
        private static KeyCertificatePair GenerateKeyCertificatePair(string dir = "")
        {
            var certificateStore = new CertificateStore(dir);
            //TODO: need swk to review
            string certificate = certificateStore.GetCertificate("cross_chain");
            if (certificate == null)
                throw new CertificateException("Unable to load Certificate.");
            string privateKey = certificateStore.GetPrivateKey("cross_chain");
            if (privateKey == null)
                throw new PrivateKeyException("Unable to load private key.");
            return new KeyCertificatePair(certificate, privateKey);
        }

        public static global::Grpc.Core.Server CreateServer(CrossChainBlockDataRpcServer crossChainBlockDataRpcServer,
            GrpcConfigOption grpcConfigOption, string dir)
        {
            var server = new global::Grpc.Core.Server
            {
                Services = {CrossChainRpc.BindService(crossChainBlockDataRpcServer)},
                Ports =
                {
                    new ServerPort(grpcConfigOption.LocalParentChainNodeIp, grpcConfigOption.LocalParentChainPort,
                        new SslServerCredentials(
                            new List<KeyCertificatePair> {GenerateKeyCertificatePair(dir)}))
                }
            };
            return server;
        }
    }
}