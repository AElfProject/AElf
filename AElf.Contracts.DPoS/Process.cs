using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using SharpRepository.Repository.Configuration;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.DPoS
{
    public class Process : CSharpSmartContract
    {
        // Set: Election.SetMiningNodes()
        public Map MiningNodes = new Map("MiningNodes");
        
        public Map TimeSlots = new Map("TimeSlots");
        
        public Map Signatures = new Map("Signatures");
        
        public Map RoundsCount = new Map("RoundsCount");

        public async Task<object> RandomizeOrderForFirstTwoRounds()
        {
            var foo = GetTime();
            var length = foo.Length;
            var bar = new int[foo.Length / 2];
            for (var i = 0; i < length; i++)
            {
                bar[i] = foo[i] + foo[length - i - 1] + new Random(17).Next(5, 99);
            }
            
            throw new NotImplementedException();
        }

        public async Task<object> GetTimeSlot(Hash accountHash)
        {
            var timeSlot = await TimeSlots.GetValue(accountHash);

            Api.Return(new BytesValue {Value = ByteString.CopyFrom(timeSlot)});
            
            return timeSlot;
        }

        public async Task<object> CanMining(Hash accountHash)
        {
            var assignedTimeSlot = (byte[]) await GetTimeSlot(accountHash);
            var timeSlotEnd = DateTime
                .Parse(Encoding.UTF8.GetString(assignedTimeSlot))
                .AddMinutes(4)
                .ToString("yyyy-MM-dd HH:mm:ss.ffffff")
                .ToUtf8Bytes();

            var can = CompareBytes(assignedTimeSlot, GetTime()) && CompareBytes(timeSlotEnd, assignedTimeSlot);
            
            Api.Return(new BoolValue {Value = can});
            
            return can;
        }
        
        public async Task<object> CalculateSignature(Hash accountHash)
        {
            throw new NotImplementedException();
        }
        
        public async Task<object> GetMiningNodes()
        {
            var miningNodes = AElf.Kernel.MiningNodes.Parser.ParseFrom(
                await MiningNodes.GetValue(Hash.Zero));

            if (miningNodes.Nodes.Count < 1)
            {
                throw new ConfigurationErrorsException("No mining nodes.");
            }
            
            Api.Return(miningNodes);

            return miningNodes;
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