using System;

namespace NCalc.Handlers
{
    public class ParameterArgs : EventArgs
    {
        private object? _result;

        public ParameterArgs(Guid id)
        {
            Id = id;
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

        public bool HasResult {get; private set;}
    }
}