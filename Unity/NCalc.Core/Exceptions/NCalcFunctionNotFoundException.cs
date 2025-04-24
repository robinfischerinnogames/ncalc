namespace NCalc.Exceptions
{
    public sealed class NCalcFunctionNotFoundException : NCalcEvaluationException
    {
        public NCalcFunctionNotFoundException(string functionName) : base($"Function not found. Name: {functionName}")
        {
            FunctionName = functionName;
        }

        private NCalcFunctionNotFoundException(string message, System.Exception exception) : base(message, exception)
        {
            FunctionName = string.Empty;
        }

        public NCalcFunctionNotFoundException() : base()
        {
            FunctionName = string.Empty;
        }

        public string FunctionName {get;}
    }
}