using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

// ReSharper disable once CheckNamespace
namespace AElf.Common
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Get computed value of Expression.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public static object GetValue(this Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)expression).Value;

                case ExpressionType.MemberAccess:
                    var memberExpr = (MemberExpression)expression;
                    {
                        object instance = memberExpr.Expression.GetValue();
                        switch (memberExpr.Member)
                        {
                            case FieldInfo field:
                                return field.GetValue(instance);

                            case PropertyInfo property:
                                return property.GetValue(instance);
                        }
                    }
                    break;

                case ExpressionType.Convert:
                    var convertExpr = (UnaryExpression)expression;
                    {
                        if (convertExpr.Method == null)
                        {
                            Type type = Nullable.GetUnderlyingType(convertExpr.Type) ?? convertExpr.Type;
                            object value = convertExpr.Operand.GetValue();
                            return Convert.ChangeType(value, type);
                        }
                    }
                    break;

                case ExpressionType.ArrayIndex:
                    var indexExpr = (BinaryExpression)expression;
                    {
                        var array = (Array)indexExpr.Left.GetValue();
                        var index = (int)indexExpr.Right.GetValue();
                        return array.GetValue(index);
                    }

                case ExpressionType.ArrayLength:
                    var lengthExpr = (UnaryExpression)expression;
                    {
                        var array = (Array)lengthExpr.Operand.GetValue();
                        return array.Length;
                    }

                case ExpressionType.Call:
                    var callExpr = (MethodCallExpression)expression;
                    {
                        if (callExpr.Method.Name == "get_Item")
                        {
                            object instance = callExpr.Object.GetValue();
                            object[] arguments = new object[callExpr.Arguments.Count];
                            for (int i = 0; i < arguments.Length; i++)
                            {
                                arguments[i] = callExpr.Arguments[i].GetValue();
                            }
                            return callExpr.Method.Invoke(instance, arguments);
                        }
                    }
                    break;
            }

            // we can't interpret the expression but we can compile and run it
            var objectMember = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);

            try
            {
                return getterLambda.Compile().Invoke();
            }
            catch (InvalidOperationException exception)
            {
                throw new NotSupportedException($"Value of {expression} can't be commuted.", exception);
            }
        }

        /// <summary>
        /// Get member name from <see cref="MemberExpression"/>.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public static string GetMemberName(this Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            if (expression.NodeType != ExpressionType.MemberAccess)
            {
                throw new NotSupportedException($"Expression {expression} is not a Member Access");
            }

            return ((MemberExpression)expression).Member.Name;
        }

        /// <summary>
        /// Create getter from <see cref="MemberExpression"/>.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public static Func<T, TProperty> CreateGetter<T, TProperty>(this Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            if (expression.NodeType != ExpressionType.MemberAccess)
            {
                throw new NotSupportedException($"Expression {expression} is not a Member Access");
            }

            var memberExpr = (MemberExpression)expression;

            switch (memberExpr.Member)
            {
                case FieldInfo field:
                    return CreateGetter<T, TProperty>(field);

                case PropertyInfo property:
                    return (Func<T, TProperty>)property.GetMethod
                        .CreateDelegate(typeof(Func<T, TProperty>));

                default:
                    throw new NotSupportedException(
                        $"Expression {expression} is not a Field or Property Access");
            }
        }

        private static Func<T, TField> CreateGetter<T, TField>(FieldInfo field)
        {
            var getterMethod = new DynamicMethod(
                string.Empty, typeof(TField), new[] { typeof(T) },
                typeof(ExpressionExtensions), true);

            var il = getterMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);

            return (Func<T, TField>)getterMethod.CreateDelegate(typeof(Func<T, TField>));
        }
    }
}
