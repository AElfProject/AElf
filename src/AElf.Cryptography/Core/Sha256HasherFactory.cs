using System.Security.Cryptography;
using AElf.Cryptography.ECVRF;

namespace AElf.Cryptography.Core
{

    public class Sha256HasherFactory : IHasherFactory
    {
        public HashAlgorithm Create()
        {
            return SHA256.Create();
        }
    }
}