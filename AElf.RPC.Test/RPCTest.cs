using System;
using System.IO;
using Google.Protobuf;
using Grpc.Core;
using Xunit;

namespace AElf.RPC.Test
{
    public class RPCTest
    {
        [Fact]
        public void SimpleRPC()
        {
            // load data 
            var data = File.ReadAllBytes("../../../contracts/Contract.dll");
            var smartContractRegistration = new SmartContractReg {Byte = ByteString.CopyFrom(data), Name = "Contract.Contract"};
            var channel = new Channel("127.0.0.1:50052", ChannelCredentials.Insecure);
            
            // create a real client
            var smartContract = new SmartContract(new AElfRPC.AElfRPCClient(channel), smartContractRegistration);
            var res = smartContract.Invoke("HelloWorld", 1);
            Console.WriteLine(res.Result.Res);

        }

        [Fact]
        public void ServerSideStream()
        {
            
        }
    }
}