using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using AElf.Common;

namespace AElf.Sdk.CSharp
{
    public class Event
    {
        private static bool IsIndexed(PropertyInfo fieldInfo)
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attributes.Length > 0;
        }

        internal LogEvent GetLogEvent(Address self = null)
        {
            var t = GetType();
            var le = new LogEvent()
            {
                Address = self
            };
            le.Topics.Add(ByteString.CopyFrom(Hash.FromString(t.Name).DumpByteArray()));
            var fields = t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(x => new {x.Name, Value = x.GetValue(this), Indexed = IsIndexed(x)})
                .ToList();
            foreach (var indexedField in fields.Where(x => x.Indexed))
            {
                le.Topics.Add(ByteString.CopyFrom(
                    SHA256.Create().ComputeHash(ParamsPacker.Pack(indexedField.Value)))
                );
            }

            var nonIndexed = fields.Where(x => !x.Indexed)
                .Select(x => x.Value).ToArray();
            le.Data = ByteString.CopyFrom(ParamsPacker.Pack(nonIndexed));
            return le;
        }
    }
}