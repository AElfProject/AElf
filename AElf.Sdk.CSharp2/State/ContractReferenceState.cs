using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AElf.Common;
using AElf.Sdk.CSharp2.State;
using Autofac.Core.Activators.Delegate;

namespace AElf.Sdk.CSharp.State
{
    public class ContractReferenceState : SingletonState<Address>
    {
        private Dictionary<string, PropertyInfo> _delegates;

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
            _delegates = this.GetType().GetProperties().Where(x => x.PropertyType.IsAction())
                .ToDictionary(x => x.Name, x => x);
        }

        private void InitializeProperties()
        {
            foreach (var kv in _delegates)
            {
                var name = kv.Key;
                var propertyInfo = kv.Value;
                var instance = new ActionInfo(this, name);
                var parameters = propertyInfo.PropertyType.GenericTypeArguments.Select(Expression.Parameter)
                    .ToArray();
                var parametersExpression = Expression.NewArrayInit(
                    typeof(object),
                    parameters.Select(x => Expression.Convert(x, typeof(object))));
                var call = Expression.Call(Expression.Constant(instance), _send, parametersExpression);
                propertyInfo.SetValue(this, Expression.Lambda(call, parameters).Compile());
            }
        }
    }
}