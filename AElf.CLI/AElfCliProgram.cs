using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Helpers;
using AElf.CLI.Http;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using AElf.CLI.Wallet.Exceptions;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Globals = AElf.Kernel.Globals;
using Method = AElf.CLI.Data.Protobuf.Method;
using Module = AElf.CLI.Data.Protobuf.Module;
using Transaction = AElf.CLI.Data.Protobuf.Transaction;

namespace AElf.CLI
{
    public class AutoCompleteWithRegisteredCommand : IAutoCompleteHandler
    {
        private readonly List<string> _commands;

        public AutoCompleteWithRegisteredCommand(List<string> commandNames)
        {
            _commands = commandNames;
        }

        public string[] GetSuggestions(string text, int index)
        {
            return _commands.Where(c => c.StartsWith(text)).ToArray();
        }

        public char[] Separators { get; set; } = { ' ' };
    }

    public class AElfCliProgram
    {
        private readonly string _rpcAddress;

        private string _genesisAddress;

        private static readonly Deserializer Deserializer = new Deserializer();
        private static List<CliCommandDefinition> _commands = new List<CliCommandDefinition>();

        private const string ExitReplCommand = "quit";
        private const string ServerConnError = "Could not connect to server.";
        private const string AbiNotLoaded = "ABI not loaded.";
        private const string NotConnected = "Please connect-blockchain first.";
        private const string InvalidTransaction = "Invalid transaction data.";
        private const string MethodNotFound = "Method not Found.";
        private const string ConnectionNeeded = "Please connect_chain first.";
        private const string NoReplyContentError = "Failed. Pleas check input.";
        private const string DeploySmartContract = "DeploySmartContract";
        private const string InvalidInputFormat = "Invalid input format.";
        private const string WrongDataFormat = "Wrong data formant.";
        private const string WrongSupportType = "Not supported type.";

        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        private readonly AccountManager _accountManager;
        private readonly Dictionary<string, Module> _loadedModules;

        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser, AccountManager accountManager, string host = "http://localhost:5000")
        {
            _rpcAddress = host;
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
            if(!ValidateCommand(parsedCmd, def)) return;

            if (def.IsLocal)
            {
                if (def is AccountCmd)
                    _accountManager.ProcessCommand(parsedCmd);

                if (def is GetDeserializedResultCmd gdr)
                    GetDeserializedResult(parsedCmd, gdr);
            }
            else
            {
                if (def is LoadContractAbiCmd)
                {
                    LoadContractAbi(parsedCmd, def);
                    return;
                }
                if (def is DeployContractCommand)
                {
                    DeployContract(parsedCmd, def);
                    return;
                }
                if (def is SendTransactionCmd)
                {
                    SendTransaction(parsedCmd, def);
                    return;
                }

                CommonRpcRequest(parsedCmd, def);
            }
        }

        private void CommonRpcRequest(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            try
            {
                if (!ValidateCommand(parsedCmd, def)) return;

                var resp=RequestRpcMethod(parsedCmd,def);
                if(resp==null) return;

                var jObj = JObject.Parse(resp);
                var j = jObj["result"];
                if (j["error"] != null)
                {
                    _screenManager.PrintLine(j["error"].ToString());
                    return;
                }

                if (j["result"]["genesis_contract"] != null)
                {
                    _genesisAddress = j["result"]["genesis_contract"].ToString();
                }

                var toPrint = def.GetPrintString(JObject.FromObject(j));
                _screenManager.PrintLine(toPrint);
            }
            catch (Exception e)
            {
                if (e is JsonReaderException)
                {
                    _screenManager.PrintError(InvalidInputFormat);
                    return;
                }
                _screenManager.PrintError(NoReplyContentError);
            }
        }

        private void GetDeserializedResult(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            try
            {
                if (!ValidateCommand(parsedCmd, def)) return;

                var t = parsedCmd.Args.ElementAt(0);
                var data = parsedCmd.Args.ElementAt(1);

                byte[] sd;
                try
                {
                    sd = ByteArrayHelpers.FromHexString(data);
                }
                catch (Exception)
                {
                    _screenManager.PrintError(WrongDataFormat);
                    return;
                }

                object dd;
                try
                {
                    dd = Deserializer.Deserialize(t, sd);
                }
                catch (Exception)
                {
                    _screenManager.PrintError(InvalidInputFormat);
                    return;
                }

                if (dd == null)
                {
                    _screenManager.PrintError(WrongSupportType);
                    return;
                }
                _screenManager.PrintLine(dd.ToString());
            }
            catch (Exception)
            {
                _screenManager.PrintError(NoReplyContentError);
            }
        }

        private void LoadContractAbi(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            try
            {
                if (!ValidateCommand(parsedCmd, def)) return;
                
                if (!parsedCmd.Args.Any())
                {
                    if (_genesisAddress == null)
                    {
                        _screenManager.PrintError(ConnectionNeeded);
                        return;
                    }
                    parsedCmd.Args.Add(_genesisAddress);
                }

                var addr = parsedCmd.Args.ElementAt(0);
                if (!_loadedModules.TryGetValue(addr, out var m))
                {
                    var resp = RequestRpcMethod(parsedCmd, def);
                    if (resp == null) return;

                    var jObj = JObject.Parse(resp);
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
                    _screenManager.PrintError(InvalidInputFormat);
                    return;
                }
                _screenManager.PrintError(NoReplyContentError);
            }
        }

        private void DeployContract(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            try
            {
                if (!ValidateCommand(parsedCmd, def)) return;

                if (_genesisAddress == null)
                {
                    _screenManager.PrintError(NotConnected);
                    return;
                }

                string filename = parsedCmd.Args.ElementAt(0);

                // Read sc bytes
                SmartContractReader screader = new SmartContractReader();
                byte[] sc = screader.Read(filename);
                string hex = sc.ToHex();

                var name = Globals.GenesisSmartContractZeroAssemblyName + Globals.GenesisSmartContractLastName;
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

                byte[] serializedParams = meth.SerializeParams(new List<string> { "1", hex });

                Transaction t = CreateTransaction(parsedCmd.Args.ElementAt(2), _genesisAddress, parsedCmd.Args.ElementAt(1),
                    DeploySmartContract, serializedParams);

                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, t);
                byte[] b = ms.ToArray();
                byte[] toSig = SHA256.Create().ComputeHash(b);
                ECSigner signer = new ECSigner();
                ECKeyPair kp = _accountManager.GetKeyPair(parsedCmd.Args.ElementAt(2));
                if (kp == null)
                    throw new AccountLockedException(parsedCmd.Args.ElementAt(2));
                ECSignature signature = signer.Sign(kp, toSig);

                // Update the signature
                t.R = signature.R;
                t.S = signature.S;
                t.P = kp.PublicKey.Q.GetEncoded();

                var resp = SignAndSendTransaction(parsedCmd,def,t);
                
                JObject jObj = JObject.Parse(resp);
                string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));

                _screenManager.PrintLine(toPrint);
            }
            catch (Exception e)
            {
                if (e is InvalidTransactionException)
                {
                    _screenManager.PrintError(InvalidTransaction);
                    return;
                }
                if (e is JsonReaderException)
                {
                    _screenManager.PrintError(InvalidInputFormat);
                    return;
                }

                _screenManager.PrintError(NoReplyContentError);
            }
        }

        private void SendTransaction(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            try
            {
                JObject j = JObject.Parse(parsedCmd.Args.ElementAt(0));

                Transaction tr = ConvertFromJson(j);
                string hex = tr.To.Value.ToHex();

                if (!_loadedModules.TryGetValue(hex.Replace("0x", ""), out var m))
                {
                    if (!_loadedModules.TryGetValue("0x" + hex.Replace("0x", ""), out m))
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

                _accountManager.SignTransaction(tr);

                var resp = SignAndSendTransaction(parsedCmd,def,tr);  
                
                JObject jObj = JObject.Parse(resp);
                string toPrint = def.GetPrintString(JObject.FromObject(jObj["result"]));

                _screenManager.PrintLine(toPrint);
            }
            catch (Exception e)
            {
                if (e is JsonReaderException || e is FormatException)
                {
                    _screenManager.PrintError(InvalidInputFormat);
                    return;
                }
                _screenManager.PrintError(NoReplyContentError);
            }
        }
               
        private bool ValidateCommand(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            var error = def.Validate(parsedCmd);
            if (string.IsNullOrEmpty(error)) return true;

            _screenManager.PrintError(error);
            return false;
        }
        
        private string RequestRpcMethod(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            var reqhttp = new HttpRequestor(_rpcAddress);
            var resp = reqhttp.DoRequest(def.BuildRequest(parsedCmd).ToString());
            if (resp == null)
            {
                _screenManager.PrintError(ServerConnError);
                return null;
            }
            if (!resp.IsEmpty()) return resp;
            _screenManager.PrintError(NoReplyContentError);
            return null;
        }        

        private Transaction CreateTransaction(string elementAt, string genesisAddress, string incrementid, string methodName, byte[] serializedParams)
        {
            try
            {
                Transaction t = new Transaction();
                t.From = ByteArrayHelpers.FromHexString(elementAt);
                t.To = ByteArrayHelpers.FromHexString(genesisAddress);
                t.IncrementId = Convert.ToUInt64(incrementid);
                t.MethodName = methodName;
                t.Params = serializedParams;
                return t;
            }
            catch (Exception)
            {
                throw new InvalidTransactionException();
            }
        }

        private string SignAndSendTransaction(CmdParseResult parsedCmd, CliCommandDefinition def,Transaction tx)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);

            byte[] b = ms.ToArray();
            string payload = b.ToHex();

            var args = new List<string> {payload};
            var reqParams = new CmdParseResult
            {
                Command = parsedCmd.Command,
                Args = args
            };
           
            var resp = RequestRpcMethod(reqParams, def);
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
                tr.IncrementId = j["incr"].ToObject<ulong>();
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