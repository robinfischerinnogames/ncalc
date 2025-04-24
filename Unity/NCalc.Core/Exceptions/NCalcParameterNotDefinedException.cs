namespace NCalc.Exceptions
{
    public sealed class NCalcParameterNotDefinedException : NCalcEvaluationException
    {
        public NCalcParameterNotDefinedException(string parameterName) : base($"Parameter {parameterName} not defined.")
        {
            ParameterName = parameterName;
        }

        private NCalcParameterNotDefinedException(string message, System.Exception exception) : base(message, exception)
        {
            ParameterName = string.Empty;
        }

        public NCalcParameterNotDefinedException() : base()
        {
            ParameterName = string.Empty;
        }

        public string ParameterName {get;}
    }
}