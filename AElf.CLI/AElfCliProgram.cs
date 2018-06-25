using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Helpers;
using AElf.CLI.Http;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Misc;
using ProtoBuf;
using ServiceStack;

namespace AElf.CLI
{
    public class AElfCliProgram
    {

        private const string RpcAddress = "http://localhost:5000";
            
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private static List<CliCommandDefinition> _commands = new List<CliCommandDefinition>();
        
        private const string ExitReplCommand = "quit";
        private const string ServerConnError = "could not connect to server";
        
        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        private readonly AccountManager _accountManager;

        private readonly List<Module> _loadedModules;
        
        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser, AccountManager accountManager)
        {
            _screenManager = screenManager;
            _cmdParser = cmdParser;
            _accountManager = accountManager;
            _loadedModules = new List<Module>();

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
                if (def is LoadContractAbiCmd l)
                {
                    try
                    {
                        // RPC
                        HttpRequestor reqhttp = new HttpRequestor(RpcAddress);
                        string resp = reqhttp.DoRequest(def.BuildRequest(parsedCmd).ToString());
        
                        if (resp == null)
                        {
                            _screenManager.PrintError(ServerConnError);
                            return;
                        }
                        
                        JObject jObj = JObject.Parse(resp);
                        var res = JObject.FromObject(jObj["result"]);
                        
                        JToken ss = res["abi"];
                        byte[] aa = Convert.FromBase64String(ss.ToString());
                        
                        MemoryStream ms = new MemoryStream(aa);
                        Module m = Serializer.Deserialize<Module>(ms);
                        
                        _screenManager.Print(JsonConvert.SerializeObject(m));
                        
                        _loadedModules.Add(m);
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return;
                }
                else if (def is DeployContractCommand dcc)
                {
                    string err = dcc.Validate(parsedCmd);
                    if (!string.IsNullOrEmpty(err))
                    {
                        _screenManager.PrintLine(err);
                        return;
                    }

                    string cat = parsedCmd.Args.ElementAt(0);
                    string filename = parsedCmd.Args.ElementAt(1);
                    
                    // Read sc bytes
                    SmartContractReader screader = new SmartContractReader();
                    byte[] sc = screader.Read(filename);
                    string hex = BitConverter.ToString(sc).Replace("-", string.Empty).ToLower();

                    Module m = _loadedModules.FirstOrDefault(ld => ld.Name.Equals("AElf.Kernel.Tests.TestContractZero"));

                    if (m == null)
                    {
                        _screenManager.PrintError("Module not loaded !");
                        return;
                    }

                    Method meth = m.Methods.FirstOrDefault(mt => mt.Name.Equals("DeploySmartContract"));
                    
                    if (meth == null)
                    {
                        _screenManager.PrintError("Method not found in module !");
                        return;
                    }
                    
                    byte[] serializedParams = meth.SerializeParams(new List<string> { cat, hex } );

                    Transaction t = new Transaction();
                    t.From = ByteArrayHelpers.FromHexString(parsedCmd.Args.ElementAt(2));
                    t.To = Convert.FromBase64String(parsedCmd.Args.ElementAt(3));
                    t.MethodName = "DeploySmartContract";
                    t.Params = serializedParams;
                    
                    SignAndSendTransaction(t);

                    return;

                }
                
                // Execute
                // 2 cases : RPC command, Local command (like account management)
                if (def.IsLocal)
                {
                    if (def is SendTransactionCmd c)
                    {
                        try
                        {
                            JObject j = JObject.Parse(parsedCmd.Args.ElementAt(0));
                            Transaction tx = _accountManager.SignTransaction(j);
                        
                            SignAndSendTransaction(tx);
                        }
                        catch (Exception e) 
                        {
                            if (e is AccountLockedException acce)
                            {
                                _screenManager.PrintLine(acce.Message);
                            }
                            else
                            {
                                _screenManager.PrintLine("Error sending transaction.");
                            }
                        }
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
                    HttpRequestor reqhttp = new HttpRequestor(RpcAddress);
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

        private void SignAndSendTransaction(Transaction tx)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);
                        
            byte[] b = ms.ToArray();

            string payload = Convert.ToBase64String(b);
                        
            var reqParams = new JObject { ["rawtx"] = payload };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "broadcast_tx", 1);
                        
            // todo send raw tx
            HttpRequestor reqhttp = new HttpRequestor(RpcAddress);
            string resp = reqhttp.DoRequest(req.ToString());
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