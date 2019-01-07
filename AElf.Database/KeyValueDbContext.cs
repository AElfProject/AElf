using System;
using System.Collections.Generic;

namespace AElf.Database
{
    public abstract class KeyValueDbContext<TKeyValueDbContext>: IKeyValueDbContext
        where TKeyValueDbContext:KeyValueDbContext<TKeyValueDbContext>
    {
        protected KeyValueDbContext(IKeyValueDatabase database)
        {
            Database = database;
            _keyValueCollections=new Dictionary<string, object>();
        }

        public IKeyValueDatabase Database { get; }

        private readonly Dictionary<string, object> _keyValueCollections;
        
        public IKeyValueCollection<TValue> Collection<TValue>(string name)
        {
            _keyValueCollections.TryGetValue(name, out var collection);
            if (collection == null)
            {
                var typedCollection = new KeyValueCollection<TValue>(name, Database);
                _keyValueCollections[name] = typedCollection;
                return typedCollection;
            }
            else
            {
                var typedCollection = collection as IKeyValueCollection<TValue>;
                if(typedCollection == null)
                    throw new InvalidOperationException("Invalid TValue, check collection value type");
                return typedCollection;
            }
            
            
        }
    }
}