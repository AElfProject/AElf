using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AElf.ABI.CSharp;
using AElf.CLI.Certificate;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Helpers;
using AElf.CLI.Http;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using AElf.CLI.Screen;
using AElf.CLI.Streaming;
using AElf.CLI.Wallet;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using ServiceStack;
using Method = AElf.CLI.Data.Protobuf.Method;
using Module = AElf.CLI.Data.Protobuf.Module;
using Transaction = AElf.CLI.Data.Protobuf.Transaction;
using TransactionType = AElf.CLI.Data.Protobuf.TransactionType;
using Microsoft.AspNetCore.SignalR.Client;

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

        private readonly string _rpcAddress;
        private readonly int _port;

        private string _genesisAddress;
            
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private static readonly Deserializer Deserializer = new Deserializer();
        
        private static List<CliCommandDefinition> _commands = new List<CliCommandDefinition>();
        
        private const string ExitReplCommand = "quit";
        private const string ServerConnError = "Unable to connect to server.";
        private const string AbiNotLoaded = "ABI not loaded.";
        private const string NotConnected = "Please connect-blockchain first.";
        private const string InvalidTransaction = "Invalid transaction data.";
        private const string MethodNotFound = "Method not Found.";
        private const string ConnectionNeeded = "Please connect_chain first.";
        private const string NoReplyContentError = "Failed. Pleas check input.";
        private const string DeploySmartContract = "DeploySmartContract";
        private const string UpdateSmartContract = "UpdateSmartContract";
        private const string WrongInputFormat = "Invalid input format.";
        private const string UriFormatEroor = "Invalid uri format.";
        
        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        private readonly AccountManager _accountManager;
        private readonly CertificatManager _certificatManager;

        private readonly Dictionary<string, Module> _loadedModules;
        
        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser, AccountManager accountManager, CertificatManager certificatManager, string host = "http://localhost:5000")
        {
            _rpcAddress = host;
            _port = int.Parse(host.Split(':')[2]);
            
            _screenManager = screenManager;
            _cmdParser = cmdParser;
            _accountManager = accountManager;
            _certificatManager = certificatManager;
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

                if (command.StartsWith("sub events") )
                {
                    string[] splitOnSpaces = command.Split(' ');

                    if (splitOnSpaces.Length == 3)
                    {
                        EventMonitor mon = new EventMonitor(_port, splitOnSpaces[2]);
                        mon.Start().GetResult();
                        Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("Sub events - incorrect arguments");
                            
                    }
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
                    error = l.Validate(parsedCmd);

                    if (!string.IsNullOrEmpty(error))
                    {
                        _screenManager.PrintError(error);
                    }
                    try
                    {
                        // RPC
                        HttpRequestor reqhttp = new HttpRequestor(_rpcAddress);
                        if (!parsedCmd.Args.Any())
                        {
                            if (_genesisAddress == null)
                            {
                                _screenManager.PrintError(ConnectionNeeded);
                                return;
                            }
                            parsedCmd.Args.Add(_genesisAddress);
                            //parsedCmd.Args.Add(Globals.GenesisBasicContract);
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
                                _screenManager.PrintError(NoReplyContentError);
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
                        if (e is JsonReaderException)
                        {
                            _screenManager.PrintError(WrongInputFormat);
                            return;
                        }
                        return;
                    }

                    return;
                }
                if (def is DeployContractCommand dcc)
                {
                    if (_genesisAddress == null)
                    {
                        _screenManager.PrintError(NotConnected);
                        return;
                    }
                    
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
                        string hex = sc.ToHex();

                        var name = GlobalConfig.GenesisBasicContract;
                        Module m = _loadedModules.Values.FirstOrDefault(ld => ld.Name.Equals(name));
            
                        if (m == null)
                        {
                            _screenManager.PrintError(AbiNotLoaded);
                            return;
                        }
            
                        Method meth = m.Methods.FirstOrDefault(mt => mt.Name.Equals(DeploySmartContract));
                        
                        if (meth == null)
                        {
                            _screenManager.PrintError(MethodNotFound);
                            return;
                        }
                        
                        byte[] serializedParams = meth.SerializeParams(new List<string> {"1", filename, hex} );
            
                        Transaction t = new Transaction();
                        t = CreateTransaction(parsedCmd.Args.ElementAt(1), _genesisAddress, 
                            DeploySmartContract, serializedParams, TransactionType.ContractTransaction);

                        t = t.AddBlockReference(_rpcAddress);
                        
                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, t);
                        byte[] b = ms.ToArray();
                        byte[] toSig = SHA256.Create().ComputeHash(b);
                        ECSigner signer = new ECSigner();
                        ECSignature signature;
                        ECKeyPair kp = _accountManager.GetKeyPair(parsedCmd.Args.ElementAt(1));
                        if (kp == null)
                            throw new AccountLockedException(parsedCmd.Args.ElementAt(1));
                        signature = signer.Sign(kp, toSig);
                        
                        // Update the signature
                        t.Sig = new Signature {R = signature.R, S = signature.S, P = kp.PublicKey.Q.GetEncoded()};
                        
                        var resp = SignAndSendTransaction(t);
                        
                        if (resp == null)
                        { 
                            _screenManager.PrintError(ServerConnError);
                            return;
                        }
                        if (resp.IsEmpty())
                        {
                            _screenManager.PrintError(NoReplyContentError);
                            return;
                        }
                        JObject jObj = JObject.Parse(resp);
                        
                        string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                        _screenManager.PrintLine(toPrint);
                        return;

                    }
                    catch (Exception e)
                    {
                        if (e is ContractLoadedException || e is AccountLockedException)
                        {
                            _screenManager.PrintError(e.Message);
                            return;
                        }

                        if (e is InvalidTransactionException)
                        {
                            _screenManager.PrintError(InvalidTransaction);
                            return;
                        }
                        if (e is JsonReaderException)
                        {
                            _screenManager.PrintError(WrongInputFormat);
                            return;
                        }
                        return;
                    }
                    
                }
                
                if (def is UpdateContractCommand ucc)
                {
                    if (_genesisAddress == null)
                    {
                        _screenManager.PrintError(NotConnected);
                        return;
                    }
                    
                    try
                    {
                        string err = ucc.Validate(parsedCmd);
                        if (!string.IsNullOrEmpty(err))
                        {
                            _screenManager.PrintLine(err);
                            return;
                        }
            
                        //string cat = parsedCmd.Args.ElementAt(0);
                        string filename = parsedCmd.Args.ElementAt(1);
                        
                        // Read sc bytes
                        SmartContractReader screader = new SmartContractReader();
                        byte[] sc = screader.Read(filename);
                        string hex = sc.ToHex();

                        var name = GlobalConfig.GenesisBasicContract;
                        Module m = _loadedModules.Values.FirstOrDefault(ld => ld.Name.Equals(name));
            
                        if (m == null)
                        {
                            _screenManager.PrintError(AbiNotLoaded);
                            return;
                        }
            
                        Method meth = m.Methods.FirstOrDefault(mt => mt.Name.Equals(UpdateSmartContract));
                        
                        if (meth == null)
                        {
                            _screenManager.PrintError(MethodNotFound);
                            return;
                        }
                        
                        byte[] serializedParams = meth.SerializeParams(new List<string> {parsedCmd.Args.ElementAt(0), hex} );
            
                        Transaction t = new Transaction();
                        t = CreateTransaction(parsedCmd.Args.ElementAt(2), _genesisAddress, 
                            UpdateSmartContract, serializedParams, TransactionType.ContractTransaction);

                        t = t.AddBlockReference(_rpcAddress);
                        
                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, t);
                        byte[] b = ms.ToArray();
                        byte[] toSig = SHA256.Create().ComputeHash(b);
                        ECSigner signer = new ECSigner();
                        ECSignature signature;
                        ECKeyPair kp = _accountManager.GetKeyPair(parsedCmd.Args.ElementAt(2));
                        if (kp == null)
                            throw new AccountLockedException(parsedCmd.Args.ElementAt(2));
                        signature = signer.Sign(kp, toSig);
                        
                        // Update the signature
                        t.Sig = new Signature {R = signature.R, S = signature.S, P = kp.PublicKey.Q.GetEncoded()};
                        
                        var resp = SignAndSendTransaction(t);
                        
                        if (resp == null)
                        { 
                            _screenManager.PrintError(ServerConnError);
                            return;
                        }
                        if (resp.IsEmpty())
                        {
                            _screenManager.PrintError(NoReplyContentError);
                            return;
                        }
                        JObject jObj = JObject.Parse(resp);
                        
                        string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                        _screenManager.PrintLine(toPrint);
                        return;

                    }
                    catch (Exception e)
                    {
                        if (e is ContractLoadedException || e is AccountLockedException)
                        {
                            _screenManager.PrintError(e.Message);
                            return;
                        }

                        if (e is InvalidTransactionException)
                        {
                            _screenManager.PrintError(InvalidTransaction);
                            return;
                        }
                        if (e is JsonReaderException)
                        {
                            _screenManager.PrintError(WrongInputFormat);
                            return;
                        }
                        return;
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
                            
                            Transaction tr ;

                            tr = ConvertFromJson(j);
                            string hex = tr.To.Value.ToHex();

                            Module m = null;
                            if (!_loadedModules.TryGetValue(hex.Replace("0x", ""), out m))
                            {
                                if (!_loadedModules.TryGetValue("0x"+hex.Replace("0x", ""), out m))
                                {
                                    _screenManager.PrintError(AbiNotLoaded);
                                    return;
                                }
                            }

                            Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

                            if (method == null)
                            {
                                _screenManager.PrintError(MethodNotFound);
                                return;
                            }
                            
                            JArray p = j["params"] == null ? null : JArray.Parse(j["params"].ToString());
                            tr.Params = j["params"] == null ? null : method.SerializeParams(p.ToObject<string[]>());
                            tr.type = TransactionType.ContractTransaction;

                            tr = tr.AddBlockReference(_rpcAddress);

                            _accountManager.SignTransaction(tr);
                            var resp = SignAndSendTransaction(tr);
                            
                            if (resp == null)
                            { 
                                _screenManager.PrintError(ServerConnError);
                                return;
                            }
                            if (resp.IsEmpty())
                            {
                                _screenManager.PrintError(NoReplyContentError);
                                return;
                            }
                            JObject jObj = JObject.Parse(resp);

                            string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                            _screenManager.PrintLine(toPrint);

                        }
                        catch (Exception e)
                        {
                            if (e is AccountLockedException || e is InvalidTransactionException ||
                                e is InvalidInputException)
                                _screenManager.PrintError(e.Message);
                            if (e is JsonReaderException || e is FormatException)
                            {
                                _screenManager.PrintError(WrongInputFormat);
                                return;
                            }
                        }
                    }
                    else if (def is CallReadOnlyCmd)
                    {
                            JObject j = JObject.Parse(parsedCmd.Args.ElementAt(0));
                            
                            Transaction tr ;

                            tr = ConvertFromJson(j);
                            string hex = tr.To.Value.ToHex();

                            Module m = null;
                            if (!_loadedModules.TryGetValue(hex.Replace("0x", ""), out m))
                            {
                                if (!_loadedModules.TryGetValue("0x"+hex.Replace("0x", ""), out m))
                                {
                                    _screenManager.PrintError(AbiNotLoaded);
                                    return;
                                }
                            }

                            Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

                            if (method == null)
                            {
                                _screenManager.PrintError(MethodNotFound);
                                return;
                            }
                            
                            JArray p = j["params"] == null ? null : JArray.Parse(j["params"].ToString());
                            tr.Params = j["params"] == null ? null : method.SerializeParams(p.ToObject<string[]>());

                            var resp = CallTransaction(tr);
                            
                            if (resp == null)
                            { 
                                _screenManager.PrintError(ServerConnError);
                                return;
                            }
                            if (resp.IsEmpty())
                            {
                                _screenManager.PrintError(NoReplyContentError);
                                return;
                            }
                            JObject jObj = JObject.Parse(resp);

                            string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));
                            _screenManager.PrintLine(toPrint);
                    }
                    else if (def is AccountCmd)
                    {
                        _accountManager.ProcessCommand(parsedCmd);
                    }
                    else if (def is CertificateCmd)
                    {
                        _certificatManager.ProcCmd(parsedCmd);
                    }
                }
                else
                {
                    try
                    {
                        // RPC
                        HttpRequestor reqhttp = new HttpRequestor(_rpcAddress);
                        string resp = reqhttp.DoRequest(def.BuildRequest(parsedCmd).ToString(), def.GetUrl());
                        
                        if (resp == null)
                        { 
                            _screenManager.PrintError(ServerConnError);
                            return;
                        }
                        
                        if (resp.IsEmpty())
                        {
                            _screenManager.PrintError(NoReplyContentError);
                            return;
                        }
                    
                        JObject jObj = JObject.Parse(resp);
                        
                        var j = jObj["result"];
                        
                        if (j["error"] != null)
                        {
                            _screenManager.PrintLine(j["error"].ToString());
                            return;
                        }
                        
                        if (j["result"]?["BasicContractZero"] != null)
                        {
                            _genesisAddress = j["result"]["BasicContractZero"].ToString();
                        }
                        
                        string toPrint = def.GetPrintString(JObject.FromObject(j));
                        
                        _screenManager.PrintLine(toPrint);
                    }
                    catch (Exception e)
                    {
                        if (e is UriFormatException)
                        {
                            _screenManager.PrintError(UriFormatEroor);
                            return;
                        }
                        
                        if (e is JsonReaderException)
                        {
                            _screenManager.PrintError(WrongInputFormat);
                            return;
                        }
                    }
                    
                }
            }
        }

        private Transaction CreateTransaction(string elementAt, string genesisAddress,
            string methodName, byte[] serializedParams, TransactionType contracttransaction)
        {
            try
            {
                Transaction t = new Transaction();
                t.From = ByteArrayHelpers.FromHexString(elementAt);
                t.To = ByteArrayHelpers.FromHexString(genesisAddress);
                t.MethodName = methodName;
                t.Params = serializedParams;
                t.type = contracttransaction;
                return t;
            }
            catch (Exception e)
            {
                throw new InvalidTransactionException();
            }
        }

        private string SignAndSendTransaction(Transaction tx)
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

            return resp;
        }
        
        private string CallTransaction(Transaction tx)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);
                        
            byte[] b = ms.ToArray();
            string payload = b.ToHex();
            var reqParams = new JObject { ["rawtx"] = payload };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "call", 1);
                        
            // todo send raw tx
            HttpRequestor reqhttp = new HttpRequestor(_rpcAddress);
            string resp = reqhttp.DoRequest(req.ToString());

            return resp;
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

        private Transaction ConvertFromJson(JObject j)
        {
            try
            {
                Transaction tr = new Transaction();
                tr.From = ByteArrayHelpers.FromHexString(j["from"].ToString());
                tr.To = ByteArrayHelpers.FromHexString(j["to"].ToString());
                tr.MethodName = j["method"].ToObject<string>();
                return tr;
            }
            catch (Exception e)
            {
                throw new InvalidTransactionException();
            }
        }
    }
}