using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Types;

namespace AElf.WebApp.MessageQueue.Dtos
{
    public class EventFilterSet
    {
        public EventFilterSet(string address, string name, Guid filterId)
        {
            Address = address;
            Name = name;
            FilterIds = new List<Guid>
            {
                filterId
            };
            Bloom = new LogEvent
            {
                Address = Types.Address.FromBase58(Address),
                Name = name
            }.GetBloom();
        }
        public string Address { get;}
        public string Name { get;}
        public Bloom Bloom { get;}
        public List<Guid> FilterIds { get;}

        public bool IsEqualToEvent(string eventAddress, string eventName)
        {
            return Address == eventAddress && Name == eventName;
        }
    }
}