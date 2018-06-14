using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using ProtoBuf;

namespace AElf.CLI.Wallet
{
    public class AccountManager
    {
        private const string NewCmdName = "new";
        private const string ListAccountsCmdName = "list";
        private const string UnlockAccountCmdName = "unlock";
        
        private AElfKeyStore _keyStore;
        private ScreenManager _screenManager;

        public AccountManager(AElfKeyStore keyStore, ScreenManager screenManager)
        {
            _screenManager = screenManager;
            _keyStore = keyStore;
        }
        
        public void ProcessCommand(CmdParseResult parsedCmd)
        {
            string subCommand = parsedCmd.Args.ElementAt(0);

            if (subCommand.Equals(NewCmdName, StringComparison.OrdinalIgnoreCase))
            {
                CreateNewAccount();
            }
            else if (subCommand.Equals(ListAccountsCmdName, StringComparison.OrdinalIgnoreCase))
            {
                ListAccounts();
            }
            else if (subCommand.Equals(UnlockAccountCmdName, StringComparison.OrdinalIgnoreCase))
            {
                UnlockAccount(parsedCmd.Args.ElementAt(1));
            }
        }

        private void UnlockAccount(string address)
        {
            var password = _screenManager.AskInvisible("password: ");
            _keyStore.OpenAsync(address, password);
        }

        private void CreateNewAccount()
        {
            var password = _screenManager.AskInvisible("password: ");
            _keyStore.Create(password);
        }

        private void ListAccounts()
        {
            List<string> accnts = _keyStore.ListAccounts();

            for (int i = 0; i < accnts.Count; i++)
            {
                _screenManager.PrintLine("account #" + i + " : " + accnts.ElementAt(i));
            }
        }

        public Transaction SignTransaction(JObject t)
        {
            Transaction tr = new Transaction();

            string addr = t["from"].ToString();
            
            //UnlockAccount(addr);
            ECKeyPair kp = _keyStore.GetAccountKeyPair(addr);

            try
            {
                tr.From = Convert.FromBase64String(addr);
                tr.To = Convert.FromBase64String(t["to"].ToString());
                tr.IncrementId = t["incr"].ToObject<ulong>();
                
                MemoryStream ms = new MemoryStream();
                Serializer.Serialize(ms, tr);
    
                byte[] b = ms.ToArray();
                byte[] toSig = SHA256.Create().ComputeHash(b);
                
                // Sign the hash
                ECSigner signer = new ECSigner();
                ECSignature signature = signer.Sign(kp, toSig);
                
                // Update the signature
                tr.R = signature.R;
                tr.S = signature.S;
                
                tr.P = kp.PublicKey.Q.GetEncoded();

                return tr;
            }
            catch (Exception e)
            {
                ;
            }
            
            return null;
        }
    }
}