using NCalc.Visitors;

namespace NCalc.Domain
{
    public sealed class BinaryExpression : LogicalExpression
    {
        public BinaryExpression(BinaryExpressionType type,
            LogicalExpression leftExpression,
            LogicalExpression rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
            Type = type;
        }

        public LogicalExpression LeftExpression {get; set;}

        public LogicalExpression RightExpression {get; set;}

        public BinaryExpressionType Type {get; set;}

        public override T Accept<T>(ILogicalExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}