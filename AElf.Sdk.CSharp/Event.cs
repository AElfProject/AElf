using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using AElf.Common;

namespace AElf.Sdk.CSharp
{
    public abstract class Event
    {
    }

    public static class EventParser<TEvent>
        where TEvent : Event
    {
        private static readonly Lazy<CacheContainer<TEvent>> _cacheContainer =
            new Lazy<CacheContainer<TEvent>>(CreateCache);


        public static LogEvent ToLogEvent(TEvent e, Address self = null)
        {
            var le = new LogEvent()
            {
                Address = self
            };

            var container = _cacheContainer.Value;

            le.Topics.Add(ByteString.CopyFrom(Hash.FromString(container.EventName).DumpByteArray()));

            foreach (var indexedField in container.Indexes)
            {
                le.Topics.Add(ByteString.CopyFrom(
                    SHA256.Create().ComputeHash(ParamsPacker.Pack(indexedField.Function(e))))
                );
            }

            var nonIndexed = container.NonIndexes.Select(x => x.Function(e)).ToArray();
            le.Data = ByteString.CopyFrom(ParamsPacker.Pack(nonIndexed));
            return le;
        }

        class TypeCache<T>
            where T : Event
        {
            public Func<T, object> Function { get; set; }
            public string Name { get; set; }
            public bool Indexed { get; set; }
        }

        class CacheContainer<T>
            where T : Event

        {
            public List<TypeCache<T>> Indexes { get; set; }
            public List<TypeCache<T>> NonIndexes { get; set; }

            public string EventName { get; set; }
        }

        private static CacheContainer<TEvent> CreateCache()
        {
            var t = typeof(TEvent);
            var fields = t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(x =>
                    new TypeCache<TEvent>
                    {
                        Function = CreateGetFuncFor<TEvent>(x.Name),
                        Name = x.Name,
                        Indexed = IsIndexed(x)
                    })
                .ToList();
            return new CacheContainer<TEvent>()
            {
                Indexes = fields.Where(p => p.Indexed).ToList(),
                NonIndexes = fields.Where(p => !p.Indexed).ToList(),
                EventName = t.Name
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static Func<T, object> CreateGetFuncFor<T>(string propertyName)
        {
            PropertyInfo prop = typeof(T).GetProperty(propertyName);

            var methodInfo = prop.GetGetMethod();

            Func<T, object> del;
            
            if (methodInfo.ReturnType.IsValueType)
            {
                //TODO: find a better way to increase the performance
                del = o => prop.GetValue(o);
            }
            else
            {
                del = (Func<T, object>) Delegate.CreateDelegate(typeof(Func<T, object>),
                    null,
                    methodInfo);
            }
            return del;
        }

        private static bool IsIndexed(PropertyInfo fieldInfo)
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attributes.Length > 0;
        }
    }
}