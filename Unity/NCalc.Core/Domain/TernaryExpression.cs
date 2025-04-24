using NCalc.Visitors;

namespace NCalc.Domain
{
    public sealed class TernaryExpression : LogicalExpression
    {
        public TernaryExpression(LogicalExpression leftExpression,
            LogicalExpression middleExpression,
            LogicalExpression rightExpression)
        {
            LeftExpression = leftExpression;
            MiddleExpression = middleExpression;
            RightExpression = rightExpression;
        }

        public LogicalExpression LeftExpression {get; set;}

        public LogicalExpression MiddleExpression {get; set;}

        public LogicalExpression RightExpression {get; set;}

        public override T Accept<T>(ILogicalExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}