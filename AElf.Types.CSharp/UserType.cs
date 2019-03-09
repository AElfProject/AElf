using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using AElf.Types.CSharp;

namespace AElf.Types.CSharp
{
    public class UserType
    {
        private static ConcurrentDictionary<System.Type, List<UserFieldInfo>> _fieldInfosByType 
            = new ConcurrentDictionary<System.Type, List<UserFieldInfo>>();

        public UserTypeHolder Pack()
        {
            var holder = new UserTypeHolder();
            foreach (var fi in GetFieldInfos())
            {
                var val = fi.FieldInfo.GetValue(this);
                holder.Fields.Add(fi.Name, fi.Packer.Pack(val));
            }
            return holder;
        }

        public void Unpack(UserTypeHolder holder)
        {
            foreach (var fi in GetFieldInfos())
            {
                var val = fi.Packer.Unpack(holder.Fields[fi.Name]);
                fi.FieldInfo.SetValue(this, val);
            }
        }

        private List<UserFieldInfo> GetFieldInfos()
        {
            var type = GetType();
            if (!_fieldInfosByType.TryGetValue(type, out var fieldInfos))
            {
                fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                                      .Select(x => new UserFieldInfo(x)).ToList();
                _fieldInfosByType.TryAdd(type, fieldInfos);
            }
            return fieldInfos;
        }

        protected bool Equals(UserType other)
        {
            if (ReferenceEquals(other, null)) {
                return false;
            }
            if (ReferenceEquals(other, this)) {
                return true;
            }
            return this.Pack().Equals(other.Pack());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UserType) obj);
        }

        public override int GetHashCode()
        {
            return this.Pack().GetHashCode();
        }
    }

    internal class Packer
    {
        public Packer(Func<object, Any> pack, Func<Any, object> unpack)
        {
            Pack = pack;
            Unpack = unpack;
        }
        public Func<object, Any> Pack { get; }
        public Func<Any, object> Unpack { get; }
    }

    internal class UserFieldInfo
    {
        private static readonly Dictionary<System.Type, Packer> _packers = new Dictionary<System.Type, Packer>(){
            {typeof(bool), new Packer((obj) => ((bool)obj).ToAny(), (any) => any.AnyToBool())},
            {typeof(int), new Packer((obj) => ((int)obj).ToAny(),(any) => any.AnyToInt32())},
            {typeof(uint), new Packer((obj) => ((uint)obj).ToAny(),(any) => any.AnyToUInt32())},
            {typeof(long), new Packer((obj) => ((long)obj).ToAny(),(any) => any.AnyToInt64())},
            {typeof(ulong), new Packer((obj) => ((ulong)obj).ToAny(),(any) => any.AnyToUInt64())},
            {typeof(string), new Packer((obj) => ((string)obj).ToAny(),(any) => any.AnyToString())},
            {typeof(byte[]), new Packer((obj) => ((byte[])obj).ToAny(),(any) => any.AnyToBytes())}
        };

        public UserFieldInfo(FieldInfo fieldInfo)
        {
            Name = fieldInfo.Name.Replace("<", "").Replace(">k__BackingField", "");
            FieldInfo = fieldInfo;
            Packer = GetPacker(fieldInfo.FieldType);
        }

        public string Name { get; }
        public FieldInfo FieldInfo { get; }
        public Packer Packer { get; }
        private Packer GetPacker(System.Type type)
        {
            string typeName = type.FullName;
            if (_packers.TryGetValue(type, out var packer))
            {
                return packer;
            }
            if (type.IsPbMessageType())
            {
                return new Packer((obj) => ((IMessage)obj).ToAny(), (any) => any.AnyToPbMessage(type));
            }
            if (type.IsUserType())
            {
                return new Packer(
                    (obj) => ((UserType)obj).ToAny(),
                    (any) =>
                    {
                        return any.AnyToUserType(type);
                    }
                );
            }
            throw new Exception("Unrecognizable field type found.");
        }
    }

}
