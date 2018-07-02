﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.ABI.CSharp;
using AElf.CLI.Command;
using AElf.CLI.Helpers;
using AElf.CLI.Http;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Misc;
using ProtoBuf;
using ServiceStack;
using Globals = AElf.Kernel.Globals;
using Method = AElf.CLI.Data.Protobuf.Method;
using Module = AElf.CLI.Data.Protobuf.Module;
using Transaction = AElf.CLI.Data.Protobuf.Transaction;
using Type = System.Type;

namespace AElf.CLI
{
    public class AutoCompleteWithRegisteredCommand : IAutoCompleteHandler
    {
        private List<string> _commands;

        public AutoCompleteWithRegisteredCommand(List<string> commandNames)
        {
            _commands = commandNames;
        }
        
        public string[] GetSuggestions(string text, int index)
        {
            return _commands.Where(c => c.StartsWith(text)).ToArray();
        }

        public char[] Separators { get; set; } = {' '};
    }
    
    public class AElfCliProgram
    {

        private string _rpcAddress;

        private string _genesisAddress;
            
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private static readonly Deserializer Deserializer = new Deserializer();
        
        private static List<CliCommandDefinition> _commands = new List<CliCommandDefinition>();
        
        private const string ExitReplCommand = "quit";
        private const string ServerConnError = "could not connect to server";
        
        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        private readonly AccountManager _accountManager;

        private readonly Dictionary<string, Module> _loadedModules;
        
        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser, AccountManager accountManager,
            int port = 5000)
        {
            _rpcAddress = "http://localhost:" + port;
            
            _screenManager = screenManager;
            _cmdParser = cmdParser;
            _accountManager = accountManager;
            _loadedModules = new Dictionary<string, Module>();

            _commands = new List<CliCommandDefinition>();
        }
        
        public void StartRepl()
        {
            _screenManager.PrintHeader();
            _screenManager.PrintUsage();
            _screenManager.PrintLine();
            
            ReadLine.AutoCompletionHandler = new AutoCompleteWithRegisteredCommand(_commands.Select(c => c.Name).ToList());
            
            while (true)
            {
                //string command = _screenManager.GetCommand();
                string command = ReadLine.Read("aelf> ");
                
                
                if (string.IsNullOrWhiteSpace(command))
                    continue;
                    
                ReadLine.AddHistory(command);

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
                if (def is GetDeserializedResultCmd g)
                {
                    try
                    {
                        var str = g.Validate(parsedCmd);
                        if (str != null)
                        {
                            _screenManager.PrintError(str);
                            return;
                        }
                        
                        // RPC
                        var t = parsedCmd.Args.ElementAt(0);
                        var data = parsedCmd.Args.ElementAt(1);

                        byte[] sd;
                        try
                        {
                            sd = ByteArrayHelpers.FromHexString(data);
                        }
                        catch (Exception e)
                        {
                            _screenManager.PrintError("Wrong data formant.");
                            return;
                        }

                        object dd;
                        try
                        {
                            dd = Deserializer.Deserialize(t, sd);
                        }
                        catch (Exception e)
                        {
                            _screenManager.PrintError("Invalid data format");
                            return;
                        }
                        if (dd == null)
                        {
                            _screenManager.PrintError("Not supported type.");
                            return;
                        }
                        _screenManager.PrintLine(dd.ToString());
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return;
                    }
                }
                
                if (def is LoadContractAbiCmd l)
                {
                    try
                    {
                        // RPC
                        HttpRequestor reqhttp = new HttpRequestor(_rpcAddress);
                        if (!parsedCmd.Args.Any())
                        {
                            if (_genesisAddress == null)
                            {
                                _screenManager.PrintError("Please connect-blockchain first!");
                                return;
                            }
                            parsedCmd.Args.Add(_genesisAddress);
                        }

                        var addr = parsedCmd.Args.ElementAt(0);
                        Module m = null;
                        if (!_loadedModules.TryGetValue(addr, out m))
                        {
                            string resp = reqhttp.DoRequest(def.BuildRequest(parsedCmd).ToString());
        
                            if (resp == null)
                            { 
                                _screenManager.PrintError(ServerConnError);
                                return;
                            }

                            if (resp.IsEmpty())
                            {
                                _screenManager.PrintError("Address not Found or wrong format");
                                return;
                            }
                            JObject jObj = JObject.Parse(resp);
                            var res = JObject.FromObject(jObj["result"]);
                        
                            JToken ss = res["abi"];
                            byte[] aa = ByteArrayHelpers.FromHexString(ss.ToString());
                        
                            MemoryStream ms = new MemoryStream(aa);
                            m = Serializer.Deserialize<Module>(ms);
                            _loadedModules.Add(addr, m);
                        }
                        
                        var obj = JObject.FromObject(m);
                        _screenManager.PrintLine(obj.ToString());
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return;
                }
                if (def is DeployContractCommand dcc)
                {
                    try
                    {
                        string err = dcc.Validate(parsedCmd);
                        if (!string.IsNullOrEmpty(err))
                        {
                            _screenManager.PrintLine(err);
                            return;
                        }
            
                        //string cat = parsedCmd.Args.ElementAt(0);
                        string filename = parsedCmd.Args.ElementAt(0);
                        
                        // Read sc bytes
                        SmartContractReader screader = new SmartContractReader();
                        byte[] sc = screader.Read(filename);
                        string hex = BitConverter.ToString(sc).Replace("-", string.Empty).ToLower();

                        var name = Globals.GenesisSmartContractZeroAssemblyName + Globals.GenesisSmartContractLastName;
                        //var name = "AElf.Kernel.Tests.TestContractZero";
                        Module m = _loadedModules.Values.FirstOrDefault(ld => ld.Name.Equals(name));
            
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

                        if (_genesisAddress == null)
                        {
                            _screenManager.PrintError("Please connect-blockchain first!");
                            return;
                        }
                        
                        byte[] serializedParams = meth.SerializeParams(new List<string> {"1", hex } );
            
                        Transaction t = new Transaction();
                        t.From = ByteArrayHelpers.FromHexString(parsedCmd.Args.ElementAt(2));
                        t.To = ByteArrayHelpers.FromHexString(_genesisAddress);
                        t.IncrementId = Convert.ToUInt64(parsedCmd.Args.ElementAt(1));
                        t.MethodName = "DeploySmartContract";
                        t.Params = serializedParams;
                        
                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, t);
            
                        byte[] b = ms.ToArray();
                        byte[] toSig = SHA256.Create().ComputeHash(b);
            
                        ECKeyPair kp = _accountManager.GetKeyPair(parsedCmd.Args.ElementAt(2));
                        
                        // Sign the hash
                        ECSigner signer = new ECSigner();

                        ECSignature signature;
                        try
                        {
                            signature  = signer.Sign(kp, toSig);
                        }
                        catch (NullReferenceException e)
                        {
                            Console.WriteLine("Account locked! Please Use CMD: account unlock  <address>");
                            return;
                        }
                        
                    
                        // Update the signature
                        t.R = signature.R;
                        t.S = signature.S;
                    
                        t.P = kp.PublicKey.Q.GetEncoded();
                        
                        var jObj = SignAndSendTransaction(t);
                                
                        string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                        _screenManager.PrintLine(toPrint);
                        return;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        //throw;
                    }
                    

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
                            
                            Transaction tr = new Transaction();

                            try
                            {
                                tr.From = ByteArrayHelpers.FromHexString(j["from"].ToString());
                                tr.To = ByteArrayHelpers.FromHexString(j["to"].ToString());
                                tr.IncrementId = j["incr"].ToObject<ulong>();
                                tr.MethodName = j["method"].ToObject<string>();

                                JArray p = JArray.Parse(j["params"].ToString());

                                string hex = BitConverter.ToString(tr.To.Value).Replace("-", string.Empty).ToLower();

                                Module m = null;
                                if (!_loadedModules.TryGetValue(hex, out m))
                                {
                                    _screenManager.PrintError("Module not loaded !");
                                    return;
                                }

                                //Module m = _loadedModules?.FirstOrDefault(ld => ld.Key.Equals(hex));
                                Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

                                if (method == null)
                                {
                                    _screenManager.PrintError("Method not Found !");
                                    return;
                                }

                                tr.Params = method.SerializeParams(p.ToObject<string[]>());

                                _accountManager.SignTransaction(tr);

                                var jObj = SignAndSendTransaction(tr);

                                string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                                _screenManager.PrintLine(toPrint);
                            }
                            catch (AccountLockedException e)
                            {
                                _screenManager.PrintError("Please unlock account!");
                                return;
                            }
                            catch (InvalidInputException e)
                            {
                                _screenManager.PrintError("Invalid input!");
                                return;
                            }
                            catch (Exception e)
                            {
                                return;
                            }
                            
                            return;
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
                    try
                    {
                        // RPC
                        HttpRequestor reqhttp = new HttpRequestor(_rpcAddress);
                        string resp = reqhttp.DoRequest(def.BuildRequest(parsedCmd).ToString());

                        if (resp == null)
                        {
                            _screenManager.PrintError(ServerConnError);
                            return;
                        }
                    
                        JObject jObj = JObject.Parse(resp);
                        
                        var j = jObj["result"];
                        if (j["error"] != null)
                        {
                            _screenManager.PrintLine(j["error"].ToString());
                            return;
                        }
                        
                        if (j["result"]["genesis-contract"] != null)
                        {
                            _genesisAddress = j["result"]["genesis-contract"].ToString();
                        }
                        string toPrint = def.GetPrintString(JObject.FromObject(j));
                        
                        _screenManager.PrintLine(toPrint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    
                }
            }
        }

        private JObject SignAndSendTransaction(Transaction tx)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);
                        
            byte[] b = ms.ToArray();

            string payload = b.ToHex();
                        
            var reqParams = new JObject { ["rawtx"] = payload };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "broadcast_tx", 1);
                        
            // todo send raw tx
            HttpRequestor reqhttp = new HttpRequestor(_rpcAddress);
            string resp = reqhttp.DoRequest(req.ToString());
            JObject jObj = JObject.Parse(resp);

            return jObj;
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