using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Xunit;

namespace AElf.RPC.Test
{
    public class RPCTest
    {
        private Task<SmartContract> Client(String className)
        {
            // load data 
            var data = File.ReadAllBytes("../../../contracts/Contract.dll");
            var smartContractRegistration = new SmartContractReg {Byte = ByteString.CopyFrom(data), Name = className};
            var channel = new Channel("127.0.0.1:50052", ChannelCredentials.Insecure);
            
            // create a real client
            var smartContract = new SmartContract(new AElfRPC.AElfRPCClient(channel), smartContractRegistration);
            return Task.FromResult(smartContract);
        }
        
        
        [Fact]
        public void SimpleRPC()
        {
            var smartContract = Client("Contract.Contract").Result;
            var res = smartContract.Invoke("HelloWorld", 1);
            Console.WriteLine(res.Result.Res);
        }

        [Fact]
        public async Task ServerSideStream()
        {
            var smartContract = Client("Contract.ListContract").Result;
            await smartContract.ListResults("WaitSecondsTwice", 2);
        }


        [Fact]
        public async Task ClientSideStream()
        {
            var smartContract = Client("Contract.ListContract").Result;
            await smartContract.ListInvoke("WaitSecondsTwice", 2);
        }

        [Fact]
        public async Task BiDirectional()
        {
            var smartContract = Client("Contract.ListContract").Result;
            await smartContract.BiDirectional("WaitSeconds", 2);
        }
    }
}