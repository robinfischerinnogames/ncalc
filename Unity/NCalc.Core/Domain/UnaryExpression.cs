using NCalc.Visitors;

namespace NCalc.Domain
{
    public sealed class UnaryExpression : LogicalExpression
    {
        public UnaryExpression(UnaryExpressionType type, LogicalExpression expression)
        {
            Expression = expression;
            Type = type;
        }

        public LogicalExpression Expression {get; set;}

        public UnaryExpressionType Type {get; set;}

        public override T Accept<T>(ILogicalExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}