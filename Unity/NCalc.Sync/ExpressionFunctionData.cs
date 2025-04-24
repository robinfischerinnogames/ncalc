using System;
using System.Collections;
using System.Collections.Generic;

namespace NCalc
{
    public class ExpressionFunctionData : IEnumerable<Expression>
    {
        public ExpressionFunctionData(Guid id, Expression[] arguments, ExpressionContext context)
        {
            Id = id;
            Arguments = arguments;
            Context = context;
        }

        public Guid Id {get;}
        private Expression[] Arguments {get;}
        public ExpressionContext Context {get;}

        public Expression this[int index]
        {
            get => Arguments[index];
            set => Arguments[index] = value;
        }

        public IEnumerator<Expression> GetEnumerator()
        {
            return ((IEnumerable<Expression>)Arguments).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Arguments.GetEnumerator();
        }
    }
}