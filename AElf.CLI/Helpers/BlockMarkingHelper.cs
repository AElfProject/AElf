using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Http;
using AElf.CLI.Parsing;
using AElf.Common.ByteArrayHelpers;
using AElf.CLI.Data.Protobuf;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Helpers
{
    public static class BlockMarkingHelper
    {
        public static Transaction AddBlockReference(this Transaction transaction, string rpcAddress)
        {
            var height = ulong.Parse(GetBlkHeight(rpcAddress));
            var hash = GetBlkHash(rpcAddress, height.ToString());
            transaction.RefBlockNumber = height;
            transaction.RefBlockPrefix = ByteArrayHelpers.FromHexString(hash).Where((b, i) => i < 4).ToArray();
            return transaction;
        }

        private static string GetBlkHeight(string rpcAddress)
        {
            var reqhttp = new HttpRequestor(rpcAddress);
            var resp = reqhttp.DoRequest(new GetBlockHeightCmd().BuildRequest(new CmdParseResult()).ToString());
            var jObj = JObject.Parse(resp);
            return jObj["result"]["result"]["block_height"].ToString();
        }

        private static string GetBlkHash(string rpcAddress, string height)
        {
            var reqhttp = new HttpRequestor(rpcAddress);
            var cmdargs = new CmdParseResult {Args = new List<string>() {height}};
            var resp = reqhttp.DoRequest(new GetBlockInfoCmd().BuildRequest(cmdargs).ToString());
            var jObj = JObject.Parse(resp);
            return jObj["result"]["result"]["Blockhash"].ToString();
        }
    }
}