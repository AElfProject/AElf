using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.Runtime.CSharp.Tests.BadContract;

public class BadReferences
{
    private int[] _ints = new int[long.MaxValue];

    private object[] _objects = new object[6];

    private string[] _strings = new string[40 * 1024 / 128 + 1];
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
    private object EncodingObject { get; } = Encoding.UTF8; // type denied, member allowed
    private int ThreadId { get; } = Environment.CurrentManagedThreadId; // type denied, member allowed
    private DateTime DateTime { get; } = DateTime.Today; // type allowed, member denied

    private void TestSecretSharing()
    {
        SecretSharingHelper.DecodeSecret(new List<byte[]>(), new List<int>(), 2);
    }
}