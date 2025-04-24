namespace NCalc.Exceptions
{
    public class NCalcEvaluationException : NCalcException
    {
        public NCalcEvaluationException(string message) : base(message)
        {
        }

        protected NCalcEvaluationException(string message, System.Exception exception) : base(message, exception)
        {
        }

        public NCalcEvaluationException() : base()
        {
        }
    }
}