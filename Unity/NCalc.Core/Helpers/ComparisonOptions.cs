using System.Globalization;

namespace NCalc.Helpers
{
    public readonly struct ComparisonOptions
    {
        public ComparisonOptions(CultureInfo cultureInfo, ExpressionOptions options)
        {
            CultureInfo = cultureInfo;
            IsCaseInsensitive = options.HasFlag(ExpressionOptions.CaseInsensitiveStringComparer);
            IsOrdinal = options.HasFlag(ExpressionOptions.OrdinalStringComparer);
        }

        public CultureInfo CultureInfo {get;}

        public bool IsCaseInsensitive {get;}

        public bool IsOrdinal {get;}
    }
}