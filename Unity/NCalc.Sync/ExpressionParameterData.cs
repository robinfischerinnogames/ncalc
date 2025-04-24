using System;

namespace NCalc
{
    public class ExpressionParameterData
    {
        public ExpressionParameterData(Guid id, ExpressionContext context)
        {
            Id = id;
            Context = context;
        }

        public Guid Id {get;}
        public ExpressionContext Context {get;}
    }
}