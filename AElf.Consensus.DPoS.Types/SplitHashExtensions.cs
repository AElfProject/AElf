using System.Collections.Generic;
using AElf.Common;
using AElf.Cryptography;
using Moserware.Security.Cryptography;

namespace AElf.Consensus.DPoS
{
    public static class SplitHashExtensions
    {
        
        public static List<SplittedInValue> SplitInValues(this Hash inValue, List<string> publicKeys, int threshold)
        {
            var list = new List<SplittedInValue>();

            var totalShares = publicKeys.Count;
            
            var shares = SecretSplitter.SplitMessage(inValue.ToHex(), threshold, totalShares);
            foreach (var share in shares)
            {
                list.Add(new SplittedInValue());
            }
            
            return list;
        }
    }
}