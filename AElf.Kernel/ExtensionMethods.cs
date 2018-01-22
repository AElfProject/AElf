using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    /// <summary>
    /// May just ignore this file until we need some extension methods but no where to place.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Count the zero numbers of a byte[].
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static int CountOfZero(this byte[] hash)
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