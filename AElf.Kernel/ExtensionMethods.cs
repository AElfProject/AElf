using AElf.Kernel.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public static class ExtensionMethods
    {


        /// <summary>
        /// Recursively compute first two hashes as well as replace them by one hash,
        /// until there is only one hash in the list.
        /// </summary>
        /// <param name = "hashlist" ></ param >
        /// < returns > Finally return the list have only one element.</returns>
        public static List<Hash<ITransaction>> ComputeProofHash(this List<Hash<ITransaction>> hashlist)
        {
            if (hashlist.Count < 2)
                return hashlist;

            List<Hash<ITransaction>> list = new List<Hash<ITransaction>>()
            {
                new Hash<ITransaction>((hashlist[0].ToString() + hashlist[1].ToString()).GetSHA256Hash())
            };

            if (hashlist.Count > 2)
                hashlist.GetRange(2, hashlist.Count - 2).ForEach(h => list.Add(h));

            return ComputeProofHash(list);
        }

        public static int NumberOfZero(this byte[] hash)
        {
            int number = 0;
            while (hash[number] == 0)
            {
                number++;
            }
            return number;
        }
    }
}