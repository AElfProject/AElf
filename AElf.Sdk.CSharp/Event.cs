using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class Event
    {
        public void Fire()
        {
            var t = GetType();
            var le = new LogEvent()
            {
                Address = Api.GetContractAddress(),
                Topic = t.Name.CalculateHash()
            };
            var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                          .Select(x => new { Name = x.Name, Value = x.GetValue(this) })
                          .Where(x => x.Value != null && x.Value.GetType().GetInterfaces().Contains(typeof(IMessage)))
                          .Select(x => new EventField()
                          {
                              Name = x.Name,
                              Value = Any.Pack((IMessage)x.Value)
                          });
            le.Details.AddRange(fields);
            Api.FireEvent(le);
        }
    }
}
