using NCalc.Visitors;

namespace NCalc.Domain
{
    public sealed class Function : LogicalExpression
    {
        public Function(Identifier identifier, LogicalExpressionList parameters)
        {
            Identifier = identifier;
            Parameters = parameters;
        }

        public Identifier Identifier {get; set;}

        public LogicalExpressionList Parameters {get; set;}

        public override T Accept<T>(ILogicalExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}