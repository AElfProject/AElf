using System.Threading;
using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography
{
    public class OpenAccount
    {
        private Timer _timer;
        public ECKeyPair KeyPair { get; set; }

        public OpenAccount()
        {
            
        }

        public void Close()
        {
            _timer.Dispose();
        }
    }
}