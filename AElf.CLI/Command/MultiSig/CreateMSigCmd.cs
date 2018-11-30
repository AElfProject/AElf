using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Helpers;
using AElf.CLI.Parsing;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using ServiceStack;

namespace AElf.CLI.Command.MultiSig
{
    public class CreateMSigCmd : CliCommandDefinition
    {   
        private const string CommandName = "create_msig_account";
        
        public CreateMSigCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return CommandName + " <address> <executing_threshold> <proposer_threshold> <auth1> <auth2> ..";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count <= 2)
            {
                return "Wrong arguments." + GetUsage();
            }
            return null;
        }
        
        public override string GetPrintString(JObject jObj)
        {
            string hash = jObj["hash"] == null ? jObj["error"].ToString() :jObj["hash"].ToString();
            string res = jObj["hash"] == null ? "error" : "txId";
            var jobj = new JObject
            {
                [res] = hash
            };
            return jobj.ToString();
        }
    }
}