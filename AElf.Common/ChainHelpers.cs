using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace AElf.Common
{
    public static class ChainHelpers
    {
        public static int GetRandomChainId()
        {
            Random r = new Random();
            return r.Next(198535, 11316496);
        }

        public static byte[] GetChainId(ulong serialNumber)
        {
            var bytes = Encoding.UTF8.GetBytes(serialNumber + "_AElf").CalculateHash().ToArray();
            return bytes;
        }
    }
}