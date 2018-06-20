using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Http;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Misc;
using ProtoBuf;

namespace AElf.CLI
{
    public class AElfCliProgram
    {
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private static List<CliCommandDefinition> _commands = new List<CliCommandDefinition>();
        
        private const string ExitReplCommand = "quit";
        private const string ServerConnError = "could not connect to server";
        
        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        private readonly AccountManager _accountManager;
        
        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser, AccountManager accountManager)
        {
            _screenManager = screenManager;
            _cmdParser = cmdParser;
            _accountManager = accountManager;
            
            _commands = new List<CliCommandDefinition>();
        }
        
        public void StartRepl()
        {
            _screenManager.PrintHeader();
            _screenManager.PrintUsage();
            _screenManager.PrintLine();
            
            while (true)
            {
                string command = _screenManager.GetCommand();

                // stop the repl if "quit", "Quit", "QuiT", ... is encountered
                if (command.Equals(ExitReplCommand, StringComparison.OrdinalIgnoreCase))
                {
                    Stop();
                    break;
                }
                
                CmdParseResult parsedCmd = _cmdParser.Parse(command);
                CliCommandDefinition def = GetCommandDefinition(parsedCmd.Command);

                if (def == null)
                {
                    _screenManager.PrintCommandNotFound(command);
                }
                else
                {
                    ProcessCommand(parsedCmd, def);
                }
            }
        }

        private void ProcessCommand(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            string error = def.Validate(parsedCmd);

            if (!string.IsNullOrEmpty(error))
            {
                _screenManager.PrintError(error);
            }
            else
            {
                // Execute
                // 2 cases : RPC command, Local command (like account management)
                if (def.IsLocal)
                {
                    if (def is SendTransactionCmd c)
                    {
                        JObject j = JObject.Parse(parsedCmd.Args.ElementAt(0));
                        Transaction tx = _accountManager.SignTransaction(j);
                        
                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, tx);
                        
                        byte[] b = ms.ToArray();

                        string payload = Convert.ToBase64String(b);
                        
                        var reqParams = new JObject { ["rawtx"] = payload };
                        var req = JsonRpcHelpers.CreateRequest(reqParams, "broadcast_tx", 1);
                        
                        // todo send raw tx
                        HttpRequestor reqhttp = new HttpRequestor("http://localhost:5000");
                        string resp = reqhttp.DoRequest(req.ToString());
                    }
                    else if (def is BroadcastBlockCmd bc)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        _accountManager.ProcessCommand(parsedCmd);
                    }
                }
                else
                {
                    // RPC
                    HttpRequestor reqhttp = new HttpRequestor("http://localhost:5000");
                    string resp = reqhttp.DoRequest(def.BuildRequest(parsedCmd).ToString());

                    if (resp == null)
                    {
                        _screenManager.PrintError(ServerConnError);
                        return;
                    }
                    
                    JObject jObj = JObject.Parse(resp);

                    string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                    _screenManager.PrintLine(toPrint);
                }
            }
        }

        private CliCommandDefinition GetCommandDefinition(string commandName)
        {
            var cmd = _commands.FirstOrDefault(c => c.Name.Equals(commandName));
            return cmd;
        }

        public void RegisterCommand(CliCommandDefinition cmd)
        {
            _commands.Add(cmd);
        }

        private void Stop()
        {
            
        }
    }
}