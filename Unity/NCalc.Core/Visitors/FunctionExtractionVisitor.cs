using System.Collections.Generic;
using System.Linq;
using NCalc.Domain;

namespace NCalc.Visitors
{
    /// <summary>
    /// Visitor dedicated to extract <see cref="Function" /> names from a <see cref="LogicalExpression" />.
    /// </summary>
    public sealed class FunctionExtractionVisitor : ILogicalExpressionVisitor<List<string>>
    {
        public List<string> Visit(Identifier identifier)
        {
            return new List<string>();
        }

        public List<string> Visit(LogicalExpressionList list)
        {
            var functions = new List<string>();
            foreach (LogicalExpression? value in list)
            {
                if (value is Function function)
                {
                    if (!functions.Contains(function.Identifier.Name))
                    {
                        functions.Add(function.Identifier.Name);
                    }

                    foreach (LogicalExpression? parameter in function.Parameters)
                    {
                        if (parameter is not null)
                        {
                            functions.AddRange(parameter.Accept(this));
                        }
                    }
                }
                else
                {
                    functions.AddRange(value.Accept(this));
                }
            }

            return functions;
        }

        public List<string> Visit(UnaryExpression expression)
        {
            return expression.Expression.Accept(this);
        }

        public List<string> Visit(BinaryExpression expression)
        {
            List<string>? leftParameters = expression.LeftExpression.Accept(this);
            List<string>? rightParameters = expression.RightExpression.Accept(this);

            leftParameters.AddRange(rightParameters);
            return leftParameters.Distinct().ToList();
        }

        public List<string> Visit(TernaryExpression expression)
        {
            List<string>? leftParameters = expression.LeftExpression.Accept(this);
            List<string>? middleParameters = expression.MiddleExpression.Accept(this);
            List<string>? rightParameters = expression.RightExpression.Accept(this);

            leftParameters.AddRange(middleParameters);
            leftParameters.AddRange(rightParameters);
            return leftParameters.Distinct().ToList();
        }

        public List<string> Visit(Function function)
        {
            var functions = new List<string> { function.Identifier.Name };

            List<string> innerFunctions = function.Parameters.Accept(this);
            functions.AddRange(innerFunctions);

            return functions.Distinct().ToList();
        }

        public List<string> Visit(ValueExpression expression)
        {
            return new List<string>();
        }
    }
}