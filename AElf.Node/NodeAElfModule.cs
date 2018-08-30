using System;
using System.Net;
using System.Security;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Autofac;

namespace AElf.Node
{
    public class NodeAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            ECKeyPair nodeKey = null;
            if (!string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccount))
            {
                try
                {
                    var ks = new AElfKeyStore(NodeConfig.Instance.DataDir);
                    var pass = string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccountPassword)
                        ? AskInvisible(NodeConfig.Instance.NodeAccount)
                        : NodeConfig.Instance.NodeAccountPassword;
                    ks.OpenAsync(NodeConfig.Instance.NodeAccount, pass, false);

                    ManagementConfig.Instance.NodeAccountPassword = pass;
                    NodeConfig.Instance.NodeAccountPassword = pass;
                    
                    nodeKey = ks.GetAccountKeyPair(NodeConfig.Instance.NodeAccount);
                    if (nodeKey == null)
                    {
                        Console.WriteLine("Load keystore failed");
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Load keystore failed");
                }
            }

            TransactionPoolConfig.Instance.EcKeyPair = nodeKey;
        }

        public void Run(IContainer container)
        {
        }
        
        private static string AskInvisible(string prefix)
        {
            Console.Write("Node account password: ");
            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                }
            }

            Console.WriteLine();
            return new NetworkCredential("", pwd).Password;
        }
    }
}