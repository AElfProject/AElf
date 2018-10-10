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
        private static bool IsIndexed(FieldInfo fieldInfo)
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attributes.Length > 0;
        }

        // TODO: How to make this implicit
        public void Fire()
        {
            var t = GetType();
            var le = new LogEvent()
            {
                Address = Api.GetContractAddress()
            };
            le.Topics.Add(ByteString.CopyFrom(Hash.FromString(t.Name).Dump()));
            var fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(x => new {Name = x.Name, Value = x.GetValue(this), Indexed = IsIndexed(x)})
                .Where(x => x.Value != null && x.Value.GetType().GetInterfaces().Contains(typeof(IMessage))).ToList();
            foreach (var indexedField in fields.Where(x => x.Indexed))
            {
                le.Topics.Add(ByteString.CopyFrom(
                    SHA256.Create().ComputeHash(((IMessage) indexedField.Value).ToByteArray()))
                );
            }

            var nonIndexed = fields.Where(x => !x.Indexed)
                .Select(x => x.Value).ToArray();
            le.Data = ByteString.CopyFrom(ParamsPacker.Pack(nonIndexed));
            Api.FireEvent(le);
        }
    }
}