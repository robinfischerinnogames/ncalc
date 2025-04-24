using System.Globalization;
using System.Runtime.CompilerServices;

namespace NCalc.Helpers
{
    public readonly struct MathHelperOptions
    {
        private readonly ExpressionOptions _options;

        public MathHelperOptions(CultureInfo cultureInfo, ExpressionOptions options)
        {
            _options = options;
            CultureInfo = cultureInfo;
        }

        public CultureInfo CultureInfo {get;}

        public bool AllowBooleanCalculation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _options.HasFlag(ExpressionOptions.AllowBooleanCalculation);
        }

        public bool DecimalAsDefault
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _options.HasFlag(ExpressionOptions.DecimalAsDefault);
        }

        public bool OverflowProtection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _options.HasFlag(ExpressionOptions.OverflowProtection);
        }

        public bool AllowCharValues
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _options.HasFlag(ExpressionOptions.AllowCharValues);
        }

        public static implicit operator MathHelperOptions(CultureInfo cultureInfo)
        {
            return new MathHelperOptions(cultureInfo, ExpressionOptions.None);
        }
    }
}