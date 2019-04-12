using System;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace AElf.CLI.Commands
{
    public struct Account
    {
        public string Mnemonic;
        public string PrivateKey;
        public string Address;
    }
}