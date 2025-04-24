using System;
using NCalc.Domain;
using NCalc.Exceptions;
using NCalc.Parser;
using UnityEngine;

namespace NCalc.Factories
{
    /// <summary>
    /// Class responsible to create <see cref="LogicalExpression" /> objects. Parlot is used for parsing strings.
    /// </summary>
    public sealed class LogicalExpressionFactory : ILogicalExpressionFactory
    {

        private static readonly LogicalExpressionFactory Instance;

        static LogicalExpressionFactory()
        {
            Instance = new LogicalExpressionFactory();
        }

        LogicalExpression ILogicalExpressionFactory.Create(string expression, ExpressionOptions options)
        {
            try
            {
                return Create(expression, options);
            }
            catch (Exception exception)
            {
                Debug.Log($"Exceptions: {exception} Expression:{expression}");
                throw new NCalcParserException("Error parsing the expression.", exception);
            }
        }

        public static LogicalExpressionFactory GetInstance()
        {
            return Instance;
        }

        public static LogicalExpression Create(string expression, ExpressionOptions options = ExpressionOptions.None)
        {
            var parserContext = new LogicalExpressionParserContext(expression, options);
            return LogicalExpressionParser.Parse(parserContext);
        }
    }
}