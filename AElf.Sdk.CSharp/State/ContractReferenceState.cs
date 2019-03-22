using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Sdk.CSharp.State
{
    public class ContractReferenceState : SingletonState<Address>
    {
        private Dictionary<string, PropertyInfo> _actionProperties;
        private Dictionary<string, PropertyInfo> _funcProperties;
        private Dictionary<string, PropertyInfo> _methodReferenceProperties;

        private readonly MethodInfo _send =
            typeof(ActionInfo).GetMethod(nameof(ActionInfo.Send),
                BindingFlags.NonPublic | BindingFlags.Instance);

        public ContractReferenceState()
        {
            DetectPropertyInfos();
            InitializeProperties();
        }

        private void DetectPropertyInfos()
        {
            _actionProperties = this.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.PropertyType.IsAction())
                .ToDictionary(x => x.Name, x => x);
            _funcProperties = this.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.PropertyType.IsFunc())
                .ToDictionary(x => x.Name, x => x);
            _methodReferenceProperties = this.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.PropertyType.IsMethodReference())
                .ToDictionary(x => x.Name, x => x);
        }

        private void InitializeProperties()
        {
            foreach (var kv in _actionProperties)
            {
                var name = kv.Key;
                var propertyInfo = kv.Value;
                var instance = new ActionInfo(this, name);
                var parameterTypes = propertyInfo.PropertyType.GenericTypeArguments;
                propertyInfo.SetValue(this, GetDelegate(instance, _send, parameterTypes));
            }

            foreach (var kv in _funcProperties)
            {
                var name = kv.Key;
                var propertyInfo = kv.Value;
                var returnType = kv.Value.PropertyType.GenericTypeArguments.Last();
                var funcInfoGenericType = typeof(FuncInfo<>);
                var funcInfo = funcInfoGenericType.MakeGenericType(returnType);
                var instance = Activator.CreateInstance(funcInfo, new object[] {this, name});
                var methodInfo = funcInfo.GetMethod("Call", BindingFlags.NonPublic | BindingFlags.Instance);
                var parameterTypes = propertyInfo.PropertyType.GenericTypeArguments;
                parameterTypes = parameterTypes.Take(parameterTypes.Length - 1).ToArray();
                propertyInfo.SetValue(this, GetDelegate(instance, methodInfo, parameterTypes));
            }
            foreach (var kv in _methodReferenceProperties)
            {
                var name = kv.Key;
                var propertyInfo = kv.Value;
                var propertyType = kv.Value.PropertyType;
                var instance = Activator.CreateInstance(propertyType, this, name);
                propertyInfo.SetValue(this, instance);
            }
        }

        private Delegate GetDelegate(object instance, MethodInfo methodInfo, Type[] parameterTypes)
        {
            var parameters = parameterTypes.Select(Expression.Parameter).ToArray();
            var parametersExpression = Expression.NewArrayInit(typeof(object),
                parameters.Select(x => Expression.Convert(x, typeof(object))));
            var call = Expression.Call(Expression.Constant(instance), methodInfo, parametersExpression);
            return Expression.Lambda(call, parameters).Compile();
        }
    }
}