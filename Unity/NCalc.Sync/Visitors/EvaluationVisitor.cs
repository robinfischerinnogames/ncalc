using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NCalc.Domain;
using NCalc.Exceptions;
using NCalc.Handlers;
using NCalc.Helpers;
using static NCalc.Helpers.TypeHelper;

namespace NCalc.Visitors
{
    /// <summary>
    /// Class responsible to evaluating <see cref="LogicalExpression" /> objects into CLR objects.
    /// </summary>
    public class EvaluationVisitor : ILogicalExpressionVisitor<object?>
    {
        private readonly ExpressionContext _context;

        /// <summary>
        /// Class responsible to evaluating <see cref="LogicalExpression" /> objects into CLR objects.
        /// </summary>
        public EvaluationVisitor(ExpressionContext context)
        {
            _context = context;
        }

        public virtual object? Visit(TernaryExpression expression)
        {
            // Evaluates the left expression and saves the value
            var left = Convert.ToBoolean(expression.LeftExpression.Accept(this), _context.CultureInfo);

            if (left)
            {
                return expression.MiddleExpression.Accept(this);
            }

            return expression.RightExpression.Accept(this);
        }

        public virtual object? Visit(BinaryExpression expression)
        {
            var left = new Lazy<object?>(() => Evaluate(expression.LeftExpression), LazyThreadSafetyMode.None);
            var right = new Lazy<object?>(() => Evaluate(expression.RightExpression), LazyThreadSafetyMode.None);

            switch (expression.Type)
            {
                case BinaryExpressionType.And:
                    return Convert.ToBoolean(left.Value, _context.CultureInfo) &&
                           Convert.ToBoolean(right.Value, _context.CultureInfo);

                case BinaryExpressionType.Or:
                    return Convert.ToBoolean(left.Value, _context.CultureInfo) ||
                           Convert.ToBoolean(right.Value, _context.CultureInfo);

                case BinaryExpressionType.Div:
                    return IsReal(left.Value) || IsReal(right.Value)
                        ? MathHelper.Divide(left.Value, right.Value, _context)
                        : MathHelper.Divide(Convert.ToDouble(left.Value, _context.CultureInfo), right.Value,
                            _context);
                case BinaryExpressionType.Equal:
                    return Compare(left.Value, right.Value, ComparisonType.Equal);
                case BinaryExpressionType.Greater:
                    return Compare(left.Value, right.Value, ComparisonType.Greater);
                case BinaryExpressionType.GreaterOrEqual:
                    return Compare(left.Value, right.Value, ComparisonType.GreaterOrEqual);
                case BinaryExpressionType.Lesser:
                    return Compare(left.Value, right.Value, ComparisonType.Lesser);
                case BinaryExpressionType.LesserOrEqual:
                    return Compare(left.Value, right.Value, ComparisonType.LesserOrEqual);
                case BinaryExpressionType.NotEqual:
                    return Compare(left.Value, right.Value, ComparisonType.NotEqual);
                case BinaryExpressionType.Minus:
                    return MathHelper.Subtract(left.Value, right.Value, _context);
                case BinaryExpressionType.Modulo:
                    return MathHelper.Modulo(left.Value, right.Value, _context);
                case BinaryExpressionType.Plus:
                {
                    object? leftValue = left.Value;
                    object? rightValue = right.Value;

                    return EvaluationHelper.Plus(leftValue, rightValue, _context);
                }

                case BinaryExpressionType.Times:
                    return MathHelper.Multiply(left.Value, right.Value, _context);

                case BinaryExpressionType.BitwiseAnd:
                    return Convert.ToUInt64(left.Value, _context.CultureInfo) &
                           Convert.ToUInt64(right.Value, _context.CultureInfo);

                case BinaryExpressionType.BitwiseOr:
                    return Convert.ToUInt64(left.Value, _context.CultureInfo) |
                           Convert.ToUInt64(right.Value, _context.CultureInfo);

                case BinaryExpressionType.BitwiseXOr:
                    return Convert.ToUInt64(left.Value, _context.CultureInfo) ^
                           Convert.ToUInt64(right.Value, _context.CultureInfo);

                case BinaryExpressionType.LeftShift:
                    return Convert.ToUInt64(left.Value, _context.CultureInfo) <<
                           Convert.ToInt32(right.Value, _context.CultureInfo);

                case BinaryExpressionType.RightShift:
                    return Convert.ToUInt64(left.Value, _context.CultureInfo) >>
                           Convert.ToInt32(right.Value, _context.CultureInfo);

                case BinaryExpressionType.Exponentiation:
                {
                    return MathHelper.Pow(left.Value, right.Value, _context);
                }

                case BinaryExpressionType.In:
                {
                    object? rightValue = right.Value;
                    object? leftValue = left.Value;
                    return EvaluationHelper.In(rightValue, leftValue, _context);
                }

                case BinaryExpressionType.NotIn:
                {
                    object? rightValue = right.Value;
                    object? leftValue = left.Value;
                    return !EvaluationHelper.In(rightValue, leftValue, _context);
                }
                case BinaryExpressionType.Like:
                {
                    var rightValue = right.Value?.ToString();
                    var leftValue = left.Value?.ToString();

                    if (rightValue == null || leftValue == null)
                    {
                        return false;
                    }

                    return EvaluationHelper.Like(leftValue, rightValue, _context);
                }

                case BinaryExpressionType.NotLike:
                {
                    var rightValue = right.Value?.ToString();
                    var leftValue = left.Value?.ToString();

                    if (rightValue == null || leftValue == null)
                    {
                        return false;
                    }

                    return !EvaluationHelper.Like(leftValue, rightValue, _context);
                }
            }

            return null;
        }

        public virtual object? Visit(UnaryExpression expression)
        {
            // Recursively evaluates the underlying expression
            object? result = expression.Expression.Accept(this);

            return EvaluationHelper.Unary(expression, result, _context);
        }

        public virtual object? Visit(ValueExpression expression)
        {
            return expression.Value;
        }

        public virtual object? Visit(Function function)
        {
            int argsCount = function.Parameters.Count;
            var args = new Expression[argsCount];

            // Don't call parameters right now, instead let the function do it as needed.
            // Some parameters shouldn't be called, for instance, in a if(), the "not" value might be a division by zero
            // Evaluating every value could produce unexpected behaviour
            for (var i = 0; i < argsCount; i++)
            {
                args[i] = new Expression(function.Parameters[i], _context);
            }

            string functionName = function.Identifier.Name;
            var functionArgs = new FunctionArgs(function.Identifier.Id, args);

            OnEvaluateFunction(functionName, functionArgs);

            if (functionArgs.HasResult)
            {
                return functionArgs.Result;
            }

            if (_context.Functions.TryGetValue(functionName, out ExpressionFunction? expressionFunction))
            {
                return expressionFunction(new ExpressionFunctionData(function.Identifier.Id, args, _context));
            }

            return BuiltInFunctionHelper.Evaluate(functionName, args, _context);
        }

        public virtual object? Visit(Identifier identifier)
        {
            string identifierName = identifier.Name;

            var parameterArgs = new ParameterArgs(identifier.Id);

            OnEvaluateParameter(identifierName, parameterArgs);

            if (parameterArgs.HasResult)
            {
                return parameterArgs.Result;
            }

            if (_context.StaticParameters.TryGetValue(identifierName, out object? parameter))
            {
                if (parameter is Expression expression)
                {
                    //Share the parameters with child expression.
                    foreach (KeyValuePair<string, object?> p in _context.StaticParameters)
                    {
                        expression.Parameters[p.Key] = p.Value;
                    }

                    foreach (KeyValuePair<string, ExpressionParameter> p in _context.DynamicParameters)
                    {
                        expression.DynamicParameters[p.Key] = p.Value;
                    }

                    expression.EvaluateFunction += _context.EvaluateFunctionHandler;
                    expression.EvaluateParameter += _context.EvaluateParameterHandler;

                    return expression.Evaluate();
                }

                return parameter;
            }

            if (_context.DynamicParameters.TryGetValue(identifierName, out ExpressionParameter? dynamicParameter))
            {
                return dynamicParameter(new ExpressionParameterData(identifier.Id, _context));
            }

            throw new NCalcParameterNotDefinedException(identifierName);
        }

        public virtual object Visit(LogicalExpressionList list)
        {
            List<object?> result = new List<object?>();

            result.AddRange(list.Select(Evaluate));

            return result;
        }

        protected bool Compare(object? a, object? b, ComparisonType comparisonType)
        {
            if (_context.Options.HasFlag(ExpressionOptions.StrictTypeMatching) && a?.GetType() != b?.GetType())
            {
                return false;
            }

            int result = CompareUsingMostPreciseType(a, b, _context);

            return comparisonType switch
            {
                ComparisonType.Equal => result == 0,
                ComparisonType.Greater => result > 0,
                ComparisonType.GreaterOrEqual => result >= 0,
                ComparisonType.Lesser => result < 0,
                ComparisonType.LesserOrEqual => result <= 0,
                ComparisonType.NotEqual => result != 0,
                _ => throw new ArgumentOutOfRangeException(nameof(comparisonType), comparisonType, null)
            };
        }

        protected void OnEvaluateFunction(string name, FunctionArgs args)
        {
            _context.EvaluateFunctionHandler?.Invoke(name, args);
        }

        protected void OnEvaluateParameter(string name, ParameterArgs args)
        {
            _context.EvaluateParameterHandler?.Invoke(name, args);
        }

        protected object? Evaluate(LogicalExpression expression)
        {
            return expression.Accept(this);
        }
    }
}