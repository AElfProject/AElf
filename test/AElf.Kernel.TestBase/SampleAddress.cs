using AElf.Types;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AElf.Kernel
{
    public static class SampleAddress
    {
        public static readonly IReadOnlyList<Address> AddressList;

        private static readonly string[] NameArray =
        {
            "address0", "address1", "address2", "address3", "address4",
            "address5", "address6", "address7", "address8", "address9"
        };

        static SampleAddress()
        {
            AddressList = new ReadOnlyCollection<Address>(
                NameArray.Select(x =>
                    Address.FromBytes(x.ComputeHash())).ToList());
        }
    }
}