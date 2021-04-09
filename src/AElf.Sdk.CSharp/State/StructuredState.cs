using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Sdk.CSharp.State
{
    public class StructuredState : StateBase
    {
        private Dictionary<string, PropertyInfo> _propertyInfos;

        //private Dictionary<string, StateBase> _states;


        public StructuredState()
        {
            DetectPropertyInfos();
            InitializeProperties();
        }

        private void DetectPropertyInfos()
        {
            _propertyInfos = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.PropertyType.IsSubclassOf(typeof(StateBase)))
                .ToDictionary(x => x.Name, x => x);
            /*_states = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.PropertyType.IsSubclassOf(typeof(StateBase)))
                .ToDictionary(x => x.Name, x =>
                {
                    var method = x.GetGetMethod();
                    var func = (Func<StateBase>) Delegate.CreateDelegate(typeof(Func<StateBase>),
                        this,
                        x.GetGetMethod());
                    return func();
                });*/
        }

        private void InitializeProperties()
        {
            foreach (var kv in _propertyInfos)
            {
                var propertyInfo = kv.Value;
                var type = propertyInfo.PropertyType;
                var instance = Activator.CreateInstance(type);
                propertyInfo.SetValue(this, instance);
            }
        }

        internal override void OnPathSet()
        {
            foreach (var kv in _propertyInfos)
            {
                var propertyInfo = kv.Value;
                var path = this.Path.Clone();
                path.Parts.Add(kv.Key);
                ((StateBase) propertyInfo.GetValue(this)).Path = path;
            }

            base.OnPathSet();
        }

        internal override void OnContextSet()
        {
            foreach (var kv in _propertyInfos)
            {
                var propertyInfo = kv.Value;
                ((StateBase) propertyInfo.GetValue(this)).Context = Context;
            }

            base.OnContextSet();
        }

        internal override async Task Clear()
        {
            var tasks = new List<Task>();
            foreach (var kv in _propertyInfos)
            {
                var task = Task.Run(async () =>
                {
                    var propertyInfo = kv.Value;
                    await ((StateBase) propertyInfo.GetValue(this)).Clear();
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        internal override async Task<TransactionExecutingStateSet> GetChanges()
        {
            var tasks = new List<Task<TransactionExecutingStateSet>>();
            foreach (var kv in _propertyInfos)
            {
                var task = Task.Run(async () =>
                {
                    var propertyInfo = kv.Value;
                    var propertyValue = (StateBase) propertyInfo.GetValue(this);
                    return await propertyValue.GetChanges();
                });
                
                tasks.Add(task);
            }

            var states = await Task.WhenAll(tasks);
            var stateSet = new TransactionExecutingStateSet();
            foreach (var changes in states)
            {
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