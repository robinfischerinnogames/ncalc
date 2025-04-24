using Parlot;
using Parlot.Fluent;

namespace NCalc.Parser
{
    public sealed class LogicalExpressionParserContext : ParseContext
    {
        public LogicalExpressionParserContext(string text, ExpressionOptions options) : base(new Scanner(text))
        {
            Options = options;
        }

        public ExpressionOptions Options {get;}
    }
}