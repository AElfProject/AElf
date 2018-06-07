using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using ServiceStack;
using SharpRepository.Repository.Configuration;

namespace AElf.Contracts.DPoS
{
    public class Process : CSharpSmartContract
    {
        public Map MiningNodes = new Map("MiningNodes");

        public async Task<object> GenerateOrder()
        {
            var salt = GetTime();
            
            throw new NotImplementedException();
        }

        public async Task<object> GetTimeSlot(Hash accountHash)
        {
            throw new NotImplementedException();
        }
        
        public async Task<object> CalculateSignature(Hash accountHash)
        {
            throw new NotImplementedException();
        }
        
        
        
        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            // invoke
            if (member != null) await (Task<object>) member.Invoke(this, parameters);
        }
        
        public async Task<object> GetMiningNodes()
        {
            //TODO: Should set mining nodes before.
            var miningNodes = AElf.Kernel.MiningNodes.Parser.ParseFrom(
                await MiningNodes.GetValue(Hash.Zero)).Nodes.ToList();

//            using (var file = 
//                File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.DPoS/MiningNodes.txt")))
//            {
//                miningNodes = file.ReadLines().ToList();
//            }

            if (miningNodes.Count < 1)
            {
                throw new ConfigurationErrorsException("No mining nodes.");
            }

            return miningNodes;
        }

        private byte[] GetTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff").ToUtf8Bytes();
        }
    }
}