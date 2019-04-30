using System;
using System.Collections.Generic;

namespace AElf.Database
{
    public abstract class KeyValueDbContext<TKeyValueDbContext>
        where TKeyValueDbContext:KeyValueDbContext<TKeyValueDbContext>
    {
        protected KeyValueDbContext()
        {
            _keyValueCollections=new Dictionary<string, IKeyValueCollection>();
        }

        public IKeyValueDatabase<TKeyValueDbContext> Database { get; set; }

        protected readonly Dictionary<string, IKeyValueCollection> _keyValueCollections;
        
        public IKeyValueCollection Collection(string name)
        {
            _keyValueCollections.TryGetValue(name, out var collection);
            if (collection == null)
            {
                return _keyValueCollections[name] = new KeyValueCollection<TKeyValueDbContext>(name, Database);
            }
            return collection;
        }

    }
}