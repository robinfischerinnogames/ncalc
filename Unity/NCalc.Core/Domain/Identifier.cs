using System;
using NCalc.Visitors;

namespace NCalc.Domain
{
    public sealed class Identifier : LogicalExpression
    {
        public Identifier(string name)
        {
            Name = name;
        }

        public Guid Id {get;} = Guid.NewGuid();
        public string Name {get; set;}

        public override T Accept<T>(ILogicalExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}