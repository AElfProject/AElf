using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AElf.ABI.CSharp;
using AElf.CLI.Certificate;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Command.MultiSig;
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
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using ServiceStack;
using Method = AElf.CLI.Data.Protobuf.Method;
using Module = AElf.CLI.Data.Protobuf.Module;
using Transaction = AElf.CLI.Data.Protobuf.Transaction;
using TransactionType = AElf.CLI.Data.Protobuf.TransactionType;
using Microsoft.AspNetCore.SignalR.Client;
using static System.DateTime;
using Address = AElf.CLI.Data.Protobuf.Address;
using Hash = AElf.CLI.Data.Protobuf.Hash;

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
        private string _authorizationAddress;

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
        public const string UnknownError = "Unknown error.";

        #region Proposal
        private const string CreateMultiSigAccount = "CreateMultiSigAccount";
        private const string Propose = "Propose";
        private const string SayYes = "SayYes";
        private const string Release = "Release";
        private const string ProposalNotFound = "Get proposal first.";
        private readonly Dictionary<string, byte[]> _txnInProposal = new Dictionary<string, byte[]>();
        #endregion
        
        
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

                        if (!_loadedModules.TryGetValue(_genesisAddress, out var m))
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

                        byte[] serializedParams = meth.SerializeParams(new List<string> {"1", hex});
            
                        Transaction t = new Transaction();
                        t = Transaction.CreateTransaction(parsedCmd.Args.ElementAt(1), _genesisAddress, 
                            DeploySmartContract, serializedParams, TransactionType.ContractTransaction);
                      
                        var resp = SignAndSendTransaction(t);
                        
                        CheckTxnResult(resp, dcc);
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

                        if (!_loadedModules.TryGetValue(_genesisAddress, out var m))
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
                        t = Transaction.CreateTransaction(parsedCmd.Args.ElementAt(2), _genesisAddress, 
                            UpdateSmartContract, serializedParams, TransactionType.ContractTransaction);

                        var resp = SignAndSendTransaction(t);
                        CheckTxnResult(resp, ucc);
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
                if (def is CreateMSigCmd createMSig)
                {
                    try
                    {
                        string err = createMSig.Validate(parsedCmd);
                        if (!string.IsNullOrEmpty(err))
                        {
                            _screenManager.PrintLine(err);
                            return;
                        }
                        int i = 0;
                        var from = parsedCmd.Args.ElementAt(i++);
                        uint executingThreshold = UInt32.Parse(parsedCmd.Args.ElementAt(i++));
                        uint proposerThreshold = UInt32.Parse(parsedCmd.Args.ElementAt(i++));
    
                        Authorization authorization = new Authorization
                        {
                            ExecutionThreshold = executingThreshold,
                            ProposerThreshold = proposerThreshold,
                        };
                
                        uint totalWeight = 0;
                        bool canBeProposed = false;
                        for (; i < parsedCmd.Args.Count; i++)
                        {
                            JObject j = JObject.Parse(parsedCmd.Args.ElementAt(i));
                            Reviewer reviewer = new Reviewer
                            {
                                PubKey = ByteArrayHelpers.FromHexString(j["Pubkey"].ToString()),
                                Weight = Convert.ToUInt32(j["Weight"].ToString())
                            };
                            totalWeight += reviewer.Weight;
                            if (reviewer.Weight >= authorization.ProposerThreshold)
                                canBeProposed = true;
                            authorization.Reviewers.Add(reviewer);
                        }

                        if (authorization.ExecutionThreshold < authorization.ProposerThreshold ||
                            totalWeight < authorization.ExecutionThreshold || !canBeProposed)
                        {
                            _screenManager.PrintError("Invalid weight in authorization.");
                            return;
                        }
                        
                        if (_authorizationAddress == null)
                        {
                            _screenManager.PrintError(NotConnected);
                            return;
                        }
                        if (!_loadedModules.TryGetValue(_authorizationAddress, out var m))
                        {
                            _screenManager.PrintError("Authorization contract " + AbiNotLoaded);
                            return;
                        }
                
                        Method meth = m.Methods.FirstOrDefault(mt => mt.Name.Equals(CreateMultiSigAccount));
                            
                        if (meth == null)
                        {
                            _screenManager.PrintError(MethodNotFound);
                            return;
                        }
    
                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, authorization);
        
                        byte[] b = ms.ToArray();
                        byte[] serializedParams = meth.SerializeParams(new List<string> {b.ToHex()});

                        Transaction t = Transaction.CreateTransaction(from, _authorizationAddress,
                            CreateMultiSigAccount, serializedParams, TransactionType.ContractTransaction);
                        var resp = SignAndSendTransaction(t);
                        CheckTxnResult(resp, createMSig);
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

                if (def is ProposeCmd pcmd)
                {
                    try
                    {
                        string err = pcmd.Validate(parsedCmd);
                        if (!string.IsNullOrEmpty(err))
                        {
                            _screenManager.PrintLine(err);
                            return;
                        }
                        int i = 0;
                        var from = parsedCmd.Args.ElementAt(i++);
                        var msig = parsedCmd.Args.ElementAt(i++);
                        var proposalName = parsedCmd.Args.ElementAt(i++);
                        var expiredTime = UtcNow.AddSeconds(UInt64.Parse(parsedCmd.Args.ElementAt(i++)));
                        Proposal proposal = new Proposal
                        {
                            MultiSigAccount = new Address(ByteArrayHelpers.FromHexString(msig)),
                            Name = proposalName,
                            ExpiredTime = expiredTime,
                            Proposer = ByteArrayHelpers.FromHexString(from),
                            Status = ProposalStatus.ToBeDecided
                        };
                        
                        #region Generate pedning txn
                        
                        JObject j = JObject.Parse(parsedCmd.Args.ElementAt(i));
                        Transaction tr ;
                        tr = Transaction.ConvertFromJson(j);
                        string hex = tr.To.Value.ToHex();
                        Module m;
                        if (!_loadedModules.TryGetValue(hex.Replace("0x", ""), out m))
                        {
                            if (!_loadedModules.TryGetValue("0x"+hex.Replace("0x", ""), out m))
                            {
                                _screenManager.PrintError(AbiNotLoaded + "for Address " + "0x"+hex.Replace("0x", ""));
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
                        tr.Type = TransactionType.MsigTransaction;

                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, tr);
                        
                        var pendingTxn = new PendingTxn
                        {
                            ProposalName = proposalName,
                            TxnData = ms.ToArray()
                        };
                        #endregion Generate pedning txn

                        proposal.TxnData = pendingTxn;
                        
                        if (_authorizationAddress == null || !_loadedModules.TryGetValue(_authorizationAddress, out m))
                        {
                            _screenManager.PrintError("Authorization contract " + AbiNotLoaded);
                            return;
                        }
                        method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(Propose));
                        if (method == null)
                        {
                            _screenManager.PrintError(MethodNotFound);
                            return;
                        }
                        ms = new MemoryStream();
                        Serializer.Serialize(ms, proposal);
        
                        byte[] serializedParams = method.SerializeParams(new List<string> {ms.ToArray().ToHex()});

                        Transaction t = Transaction.CreateTransaction(from, _authorizationAddress,
                            Propose, serializedParams, TransactionType.ContractTransaction);
                        var resp = SignAndSendTransaction(t);
                        CheckTxnResult(resp, pcmd);
                        return;
                    }
                    catch (Exception e)
                    {
                        switch (e)
                        {
                            case ContractLoadedException _:
                            case AccountLockedException _:
                                _screenManager.PrintError(e.Message);
                                return;
                            case InvalidTransactionException _:
                                _screenManager.PrintError(InvalidTransaction);
                                return;
                            case JsonReaderException _:
                                _screenManager.PrintError(WrongInputFormat);
                                return;
                            default:
                                return;
                        }
                    }
                }
                
                if (def is ApproveCmd acmd)
                {
                    try
                    {
                        string err = acmd.Validate(parsedCmd);
                        if (!string.IsNullOrEmpty(err))
                        {
                            _screenManager.PrintLine(err);
                            return;
                        }
                        int i = 0;
                        var from = parsedCmd.Args.ElementAt(i++);
                        var proposalHash = parsedCmd.Args.ElementAt(i++);
                        if (!_txnInProposal.TryGetValue(proposalHash, out var txnData))
                        {
                            _screenManager.PrintError(ProposalNotFound + " Use " + CheckProposalCmd.CommandName + ".");
                            return;
                        }
                        if (_authorizationAddress == null || !_loadedModules.TryGetValue(_authorizationAddress, out var m))
                        {
                            _screenManager.PrintError("Authorization contract " + AbiNotLoaded);
                            return;
                        }
                        
                        Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(SayYes));
                        if (method == null)
                        {
                            _screenManager.PrintError(MethodNotFound);
                            return;
                        }
                        
                        Sig sig = _accountManager.Sign(from, txnData);

                        Approval approval = new Approval
                        {
                            ProposalHash = ByteArrayHelpers.FromHexString(proposalHash),
                            Signature = sig
                        };
                        
                        MemoryStream ms = new MemoryStream();
                        Serializer.Serialize(ms, approval);
        
                        byte[] serializedParams = method.SerializeParams(new List<string> {ms.ToArray().ToHex()});

                        Transaction t = Transaction.CreateTransaction(from, _authorizationAddress,
                            SayYes, serializedParams, TransactionType.ContractTransaction);
                        var resp = SignAndSendTransaction(t);
                        CheckTxnResult(resp, acmd);
                        return;
                    }
                    catch (Exception e)
                    {
                        switch (e)
                        {
                            case ContractLoadedException _:
                            case AccountLockedException _:
                                _screenManager.PrintError(e.Message);
                                return;
                            case InvalidTransactionException _:
                                _screenManager.PrintError(InvalidTransaction);
                                return;
                            case JsonReaderException _:
                                _screenManager.PrintError(WrongInputFormat);
                                return;
                            default:
                                return;
                        }
                    }
                }
                
                if (def is ReleaseProposalCmd rcmd)
                {
                    try
                    {
                        string err = rcmd.Validate(parsedCmd);
                        if (!string.IsNullOrEmpty(err))
                        {
                            _screenManager.PrintLine(err);
                            return;
                        }
                        int i = 0;
                        var from = parsedCmd.Args.ElementAt(i++);
                        var proposalHash = parsedCmd.Args.ElementAt(i++);
                        
                        if (_authorizationAddress == null || !_loadedModules.TryGetValue(_authorizationAddress, out var m))
                        {
                            _screenManager.PrintError("Authorization contract " + AbiNotLoaded);
                            return;
                        }
                        
                        Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(Release));
                        if (method == null)
                        {
                            _screenManager.PrintError(MethodNotFound);
                            return;
                        }

                        byte[] serializedParams = method.SerializeParams(new List<string> {ByteArrayHelpers.FromHexString(proposalHash).ToHex()});

                        Transaction t = Transaction.CreateTransaction(from, _authorizationAddress,
                            Release, serializedParams, TransactionType.ContractTransaction);
                        var resp = SignAndSendTransaction(t);
                        CheckTxnResult(resp, rcmd);
                        return;
                    }
                    catch (Exception e)
                    {
                        switch (e)
                        {
                            case ContractLoadedException _:
                            case AccountLockedException _:
                                _screenManager.PrintError(e.Message);
                                return;
                            case InvalidTransactionException _:
                                _screenManager.PrintError(InvalidTransaction);
                                return;
                            case JsonReaderException _:
                                _screenManager.PrintError(WrongInputFormat);
                                return;
                            default:
                                return;
                        }
                    }
                }
                // Execute
                // 2 cases : RPC command, Local command (like account management)
                if (def.IsLocal)
                {
                    if (def is SendTransactionCmd stc)
                    {
                        try
                        {
                            JObject j = JObject.Parse(parsedCmd.Args.ElementAt(0));
                            
                            Transaction tr ;

                            tr = Transaction.ConvertFromJson(j);
                            string hex = tr.To.Value.ToHex();

                            Module m;
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
                            tr.Type = TransactionType.ContractTransaction;

                            var resp = SignAndSendTransaction(tr);
                            CheckTxnResult(resp, stc);
                        }
                        catch (Exception e)
                        {
                            if (e is AccountLockedException || e is InvalidTransactionException ||
                                e is InvalidInputException)
                                _screenManager.PrintError(e.Message);
                            if (e is JsonReaderException || e is FormatException || e is ArgumentOutOfRangeException)
                            {
                                _screenManager.PrintError(WrongInputFormat);
                            }
                            _screenManager.PrintError(UnknownError);
                        }
                    }
                    else if (def is CallReadOnlyCmd)
                    {
                        JObject j = JObject.Parse(parsedCmd.Args.ElementAt(0));
                        
                        Transaction tr ;

                        tr = Transaction.ConvertFromJson(j);
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

                        var resp = CallTransaction(tr, "call");
                        
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
                        
                        // set genesis contract address if rpc api is connect_chain
                        if (j["result"]?[GlobalConfig.GenesisSmartContractZeroAssemblyName] != null)
                        {
                            _genesisAddress = j["result"][GlobalConfig.GenesisSmartContractZeroAssemblyName].ToString();
                            _authorizationAddress = j["result"][GlobalConfig.GenesisAuthorizationContractAssemblyName].ToString();
                        }
                        
                        string toPrint = def.GetPrintString(JObject.FromObject(j));

                        if (def is CheckProposalCmd && j["result"]["TxnData"] != null)
                        {
                            var txnData = ByteArrayHelpers.FromHexString(j["result"]["TxnData"].ToString());
                            _txnInProposal.TryAdd(parsedCmd.Args.ElementAt(0), txnData);
                        }
                        
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

        private string SignAndSendTransaction(Transaction tx)
        {
            // add block reference
            tx.AddBlockReference(_rpcAddress);
            
            // sign
            _accountManager.SignTransaction(tx);
            string resp = CallTransaction(tx, "broadcast_tx");
            return resp;
        }

        private void CheckTxnResult(string resp, CliCommandDefinition def)
        {
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
        
        private string CallTransaction(Transaction tx, string api)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);
                        
            byte[] b = ms.ToArray();
            string payload = b.ToHex();
            var reqParams = new JObject { ["rawtx"] = payload };
            var req = JsonRpcHelpers.CreateRequest(reqParams, api, 1);
                        
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
    }
}