using System;

namespace NCalc.Handlers
{
    public class FunctionArgs : EventArgs
    {
        private object? _result;

        public FunctionArgs(Guid id, Expression[] parameters)
        {
            Id = id;
            Parameters = parameters;
        }

        public Guid Id {get;}

        public object? Result
        {
            get => _result;
            set
            {
                _result = value;
                HasResult = true;
            }
        }

        public Expression[] Parameters {get;}

        public bool HasResult {get; private set;}

        public object?[] EvaluateParameters()
        {
            var values = new object?[Parameters.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Parameters[i].Evaluate();
            }

            return values;
        }
    }
}