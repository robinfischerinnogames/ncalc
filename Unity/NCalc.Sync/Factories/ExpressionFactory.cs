using NCalc.Cache;
using NCalc.Domain;

namespace NCalc.Factories
{
    /// <summary>
    /// Default <see cref="IExpressionFactory" /> implementation.
    /// </summary>
    public sealed class ExpressionFactory : IExpressionFactory
    {
        private readonly ILogicalExpressionFactory _logicalExpressionFactory;
        private readonly ILogicalExpressionCache _cache;
        private readonly IEvaluationVisitorFactory _evaluationVisitorFactory;

        /// <summary>
        /// Default <see cref="IExpressionFactory" /> implementation.
        /// </summary>
        public ExpressionFactory(ILogicalExpressionFactory logicalExpressionFactory,
            ILogicalExpressionCache cache,
            IEvaluationVisitorFactory evaluationVisitorFactory)
        {
            _logicalExpressionFactory = logicalExpressionFactory;
            _cache = cache;
            _evaluationVisitorFactory = evaluationVisitorFactory;
        }

        public Expression Create(string expression, ExpressionContext? expressionContext = null)
        {
            return new Expression(expression, expressionContext ?? new ExpressionContext(), _logicalExpressionFactory,
                _cache, _evaluationVisitorFactory);
        }

        public Expression Create(LogicalExpression logicalExpression, ExpressionContext? expressionContext = null)
        {
            return new Expression(logicalExpression, expressionContext ?? new ExpressionContext(),
                _logicalExpressionFactory, _cache, _evaluationVisitorFactory);
        }
    }
}