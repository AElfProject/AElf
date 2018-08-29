using System;

namespace AElf.Miner.Rpc.Exceptions
{
    public class CertificateException : Exception
    {
        public CertificateException(string unableToLoadCertificate) : base(unableToLoadCertificate)
        {
        }
    }
}