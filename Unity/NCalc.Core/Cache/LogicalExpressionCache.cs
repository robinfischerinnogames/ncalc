using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NCalc.Domain;

namespace NCalc.Cache
{
    public sealed class LogicalExpressionCache : ILogicalExpressionCache
    {
        private static readonly LogicalExpressionCache Instance;
        private readonly ConcurrentDictionary<string, WeakReference<LogicalExpression>> _compiledExpressions = new();

        static LogicalExpressionCache()
        {
            Instance = new LogicalExpressionCache();
        }

        public bool TryGetValue(string expression, out LogicalExpression? logicalExpression)
        {
            logicalExpression = null;

            if (!_compiledExpressions.TryGetValue(expression, out WeakReference<LogicalExpression>? wr))
            {
                return false;
            }

            if (!wr.TryGetTarget(out logicalExpression))
            {
                return false;
            }


            return true;
        }

        public void Set(string expression, LogicalExpression logicalExpression)
        {
            _compiledExpressions[expression] = new WeakReference<LogicalExpression>(logicalExpression);
            ClearCache();
        }

        public static LogicalExpressionCache GetInstance()
        {
            return Instance;
        }

        private void ClearCache()
        {
            foreach (KeyValuePair<string, WeakReference<LogicalExpression>> kvp in _compiledExpressions)
            {
                if (kvp.Value.TryGetTarget(out _))
                {
                    continue;
                }

                _compiledExpressions.TryRemove(kvp.Key, out _);
            }
        }
    }
}