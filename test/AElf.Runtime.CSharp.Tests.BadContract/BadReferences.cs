using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.Runtime.CSharp.Tests.BadContract
{
    public class BadReferences
    {
        private ChainManager ChainManager { get; set; } //assembly denied
        private Random[] RandomArray { get; set; } //array with namespace denied type
        
        private List<int> AllowedListField // namespace allowed
        {
            get;
        }
        
        private List<Random> DeniedListField // namespace allowed
        {
            get;
        }
        
        private Assembly AssemblyField { get; set; } // namespace denied

        private AssemblyCompanyAttribute AssemblyCompanyAttribute { get; set; } // namespace denied, type allowed
        private Object EncodingObject { get; } = Encoding.UTF8; // type denied, member allowed
        private int ThreadId { get; } = Environment.CurrentManagedThreadId; // type denied, member allowed
        private DateTime DateTime { get; } = DateTime.Today; // type allowed, member denied

        private string[] _strings = new string[(40 * 1024) / 128 + 1];
        
        private object[] _objects = new object[6];

        private int[] _ints = new int[long.MaxValue];
        
        private void TestSecretSharing()
        {
            SecretSharingHelper.DecodeSecret(new List<byte[]>(), new List<int>(), 2);
        }
    }
}