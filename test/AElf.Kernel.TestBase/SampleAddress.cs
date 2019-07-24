using AElf.Types;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AElf.Kernel
{
    public static class SampleAddress
    {
        public static readonly IReadOnlyList<Address> AddressList;

        private static readonly string[] Base58Strings =
        {
            "2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz",
            "2ktxGpyiYCjFU5KwuXtbBckczX6uPmEtesJEsQPqMukcHZFY9a",
            "2LdrKw6vi2uWSSGhiS1MBUPANFuhJzBPYDsQ65Jm7C2uEy5KKW",
            "2ohojn441KmsVkaDS3wEL928gbpan352ZJ5ruMFxoa8iorUce",
            "2vNDCj1WjNLAXm3VnEeGGRMw3Aab4amVSEaYmCyxQKjNhLhfL7",
            "4v9cdSsn2PmZuFCxoSZhtY7Q2yUjdTNz6sQQdHNibdgaRg8Wx",
            "9Njc5pXW9Rw499wqSJzrfQuJQFVCcWnLNjZispJM4LjKmRPyq",
            "Lib8JSzdsFC7uCwvEwviadh3kp9LzaLMCauK4fSzrwc2qtHVi",
            "LYKSAU799wDphRK7W5ZsMBF2vDG8ijeuESk1R7Xpi6hBpdnX4",
            "ZJjdajAmP5HpWgvLkXa5mm6gcuGWwKjN3Kos89ZJogHYDgTsB"
        };

        static SampleAddress()
        {
            AddressList = new ReadOnlyCollection<Address>(
                Base58Strings.Select(AddressHelper.Base58StringToAddress).ToList());
        }
    }
}