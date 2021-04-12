using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Types;

namespace AElf.Sdk.CSharp.State
{
    public class StructuredState : StateBase
    {
        //private Dictionary<string, PropertyInfo> _propertyInfos;

        //private Dictionary<string, Func<StateBase>> _stateFuncs;
        private Dictionary<string, StateBase> _states;
        
        public StructuredState()
        {
            DetectPropertyInfos();
            //InitializeProperties();
        }

        private void DetectPropertyInfos()
        {
            // _propertyInfos = this.GetType()
            //     .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            //     .Where(x => x.PropertyType.IsSubclassOf(typeof(StateBase)))
            //     .ToDictionary(x => x.Name, x => x);
            _states = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.PropertyType.IsSubclassOf(typeof(StateBase)))
                .ToDictionary(x => x.Name, x =>
                {
                     var type = x.PropertyType;
                     var instance = Activator.CreateInstance(type);
                     x.SetValue(this, instance);
                    return instance as StateBase;
                    
                    // var method = x.GetMethod.Name;
                    // var del = Delegate.CreateDelegate(Type.GetType($"System.Func`1[[{x.GetMethod.ReturnType.AssemblyQualifiedName}]]"), this, method);
                    // var func = (Func<StateBase>) del;
                    // return func;
                });
        }

        // private void InitializeProperties()
        // {
        //     foreach (var kv in _propertyInfos)
        //     {
        //         var propertyInfo = kv.Value;
        //         var type = propertyInfo.PropertyType;
        //         var instance = Activator.CreateInstance(type);
        //         propertyInfo.SetValue(this, instance);
        //     }
        // }

        internal override void OnPathSet()
        {
            foreach (var kv in _states)
            {
                var path = this.Path.Clone();
                path.Parts.Add(kv.Key);
                kv.Value.Path = path;
            }

            base.OnPathSet();
        }

        internal override void OnContextSet()
        {
            foreach (var kv in _states)
            {
                var propertyInfo = kv.Value;
                kv.Value.Context = Context;
            }

            base.OnContextSet();
        }

        internal override void Clear()
        {
            foreach (var kv in _states)
            {
                var propertyInfo = kv.Value;
                kv.Value.Clear();
            }
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            foreach (var kv in _states)
            {
                var propertyValue = kv.Value;
                var changes = kv.Value.GetChanges();
                foreach (var kv1 in changes.Writes)
                {
                    stateSet.Writes[kv1.Key] = kv1.Value;
                }
                
                foreach (var kv1 in changes.Deletes)
                {
                    stateSet.Deletes[kv1.Key] = kv1.Value;
                }

                foreach (var kv1 in changes.Reads)
                {
                    stateSet.Reads[kv1.Key] = kv1.Value;
                }
            }

            return stateSet;
        }
    }
}