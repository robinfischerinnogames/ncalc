using System.Collections.Generic;
using System.Globalization;
using NCalc.Handlers;

namespace NCalc
{
    public record ExpressionContext : ExpressionContextBase
    {
        public ExpressionContext()
        {
        }

        public ExpressionContext(ExpressionOptions options, CultureInfo? cultureInfo)
        {
            Options = options;
            CultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
        }

        public IDictionary<string, ExpressionParameter> DynamicParameters {get; set;} =
            new Dictionary<string, ExpressionParameter>();

        public IDictionary<string, ExpressionFunction> Functions {get; set;} =
            new Dictionary<string, ExpressionFunction>();

        public EvaluateParameterHandler? EvaluateParameterHandler {get; set;}
        public EvaluateFunctionHandler? EvaluateFunctionHandler {get; set;}

        public static implicit operator ExpressionContext(ExpressionOptions options)
        {
            return new ExpressionContext
            {
                Options = options
            };
        }

        public static implicit operator ExpressionContext(CultureInfo cultureInfo)
        {
            return new ExpressionContext
            {
                CultureInfo = cultureInfo
            };
        }
    }
}