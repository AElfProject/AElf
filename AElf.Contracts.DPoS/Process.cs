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
        
        public Map TimeSlots = new Map("TimeSlots");
        
        public Map Signatures = new Map("Signatures");

        public async Task<object> RandomizeOrderForFirstTwoRounds()
        {
            var foo = GetTime();
            var length = foo.Length;
            var bar = new int[foo.Length / 2];
            for (var i = 0; i < length; i++)
            {
                bar[i] = foo[i] + foo[length - i - 1];
            }
            
            throw new NotImplementedException();
        }

        public async Task<object> GetTimeSlot(Hash accountHash)
        {
            return await TimeSlots.GetValue(accountHash);
        }

        public async Task<object> CanStartMining(Hash accountHash)
        {
            var assignedTimeSlot = (byte[]) await GetTimeSlot(accountHash);
            return CompareBytes(assignedTimeSlot, GetTime());
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
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

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

        private bool CompareBytes(byte[] bytes1, byte[] bytes2)
        {
            //Caonnot compare
            if (bytes1.Length != bytes2.Length)
            {
                return false;
            }

            var length = bytes1.Length;
            for (var i = 0; i < length; i++)
            {
                if (bytes1[i] > bytes2[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}