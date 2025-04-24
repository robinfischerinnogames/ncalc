using System;
using System.Globalization;
using System.Numerics;
using ExtendedNumerics;

namespace NCalc.Helpers
{
    /// <summary>
    /// Utilities for doing mathematical operations between different object types without using dynamic.
    /// </summary>
    public static class MathHelper
    {
        // Enum to represent the operation type
        private enum OperationType { Add, Subtract, Multiply, Divide, Modulo }

        public static object? Add(object? a, object? b) => Add(a, b, new MathHelperOptions());
        public static object? Add(object? a, object? b, MathHelperOptions options) =>
            PerformOperation(a, b, OperationType.Add, options);

        public static object? Subtract(object? a, object? b) => Subtract(a, b, new MathHelperOptions());
        public static object? Subtract(object? a, object? b, MathHelperOptions options) =>
            PerformOperation(a, b, OperationType.Subtract, options);

        public static object? Multiply(object? a, object? b) => Multiply(a, b, new MathHelperOptions());
        public static object? Multiply(object? a, object? b, MathHelperOptions options) =>
            PerformOperation(a, b, OperationType.Multiply, options);

        public static object? Divide(object? a, object? b) => Divide(a, b, new MathHelperOptions());
        public static object? Divide(object? a, object? b, MathHelperOptions options) =>
            PerformOperation(a, b, OperationType.Divide, options);

        public static object? Modulo(object? a, object? b) => Modulo(a, b, new MathHelperOptions());
        public static object? Modulo(object? a, object? b, MathHelperOptions options) =>
            PerformOperation(a, b, OperationType.Modulo, options);

        private static object? PerformOperation(object? a, object? b, OperationType operationType, MathHelperOptions options)
        {
            if (a == null || b == null)
            {
                return null;
            }

            try
            {
                object processedA = ConvertInput(a, options);
                object processedB = ConvertInput(b, options);

                // Special case for Modulo: Promote to Decimal for more predictable behavior across types
                // unless both inputs are already floating point (double), then use double's remainder.
                bool useDecimal = options.DecimalAsDefault || operationType == OperationType.Modulo;
                if (operationType == OperationType.Modulo && processedA is double && processedB is double)
                {
                    useDecimal = false; // Use double's remainder if both inputs are double
                }


                if (useDecimal)
                {
                    decimal decA = ConvertToDecimal(processedA, options);
                    decimal decB = ConvertToDecimal(processedB, options);

                    if (options.OverflowProtection)
                    {
                        checked
                        {
                            return operationType switch
                            {
                                OperationType.Add => decA + decB,
                                OperationType.Subtract => decA - decB,
                                OperationType.Multiply => decA * decB,
                                OperationType.Divide => decA / decB, // Throws DivideByZeroException
                                OperationType.Modulo => decA % decB, // Throws DivideByZeroException
                                _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
                            };
                        }
                    }
                    else // Unchecked
                    {
                        return operationType switch
                        {
                            OperationType.Add => decA + decB,
                            OperationType.Subtract => decA - decB,
                            OperationType.Multiply => decA * decB,
                            OperationType.Divide => decB == 0M ? throw new DivideByZeroException() : decA / decB,
                            OperationType.Modulo => decB == 0M ? throw new DivideByZeroException() : decA % decB,
                            _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
                        };
                    }
                }
                else // Use Double
                {
                    double dblA = ConvertToDouble(processedA, options);
                    double dblB = ConvertToDouble(processedB, options);
                    object result;

                    if (options.OverflowProtection)
                    {
                        checked // Primarily for intermediate integer conversions, less effective for double itself
                        {
                             result = operationType switch
                            {
                                OperationType.Add => dblA + dblB,
                                OperationType.Subtract => dblA - dblB,
                                OperationType.Multiply => dblA * dblB,
                                // Double division handles 0 denominator returning +/- Infinity or NaN
                                OperationType.Divide => dblA / dblB,
                                // Use IEEERemainder for floating point modulo behavior if preferred,
                                // but standard % aligns more with integer/decimal modulo.
                                // Double modulo handles 0 denominator returning NaN
                                OperationType.Modulo => dblA % dblB, // Or Math.IEEERemainder(dblA, dblB);
                                _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
                            };
                        }
                        CheckFloatingPointOverflow(result); // Check for Infinity post-operation
                        return result;
                    }
                    else // Unchecked
                    {
                        return operationType switch
                        {
                            OperationType.Add => dblA + dblB,
                            OperationType.Subtract => dblA - dblB,
                            OperationType.Multiply => dblA * dblB,
                            OperationType.Divide => dblA / dblB,
                            OperationType.Modulo => dblA % dblB, // Or Math.IEEERemainder(dblA, dblB);
                            _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
                        };
                    }
                }
            }
            catch (OverflowException) // Catch explicit overflow from checked blocks
            {
                throw; // Re-throw
            }
            catch (Exception ex) when (ex is not DivideByZeroException and not OverflowException) // Catch conversion or other errors
            {
                throw new InvalidOperationException(
                    $"Operator '{GetOperatorChar(operationType)}' not implemented or invalid for operands of types {a?.GetType().Name ?? "null"} and {b?.GetType().Name ?? "null"}. See inner exception for details.", ex);
            }
        }

        private static char GetOperatorChar(OperationType type) => type switch
        {
            OperationType.Add => '+',
            OperationType.Subtract => '-',
            OperationType.Multiply => '*',
            OperationType.Divide => '/',
            OperationType.Modulo => '%',
            _ => '?'
        };

        // --- Min/Max remain largely the same as they didn't use dynamic ---

        public static object? Max(object? a, object? b) => Max(a, b, new MathHelperOptions());

        public static object? Max(object? a, object? b, MathHelperOptions options)
        {
            if (a == null && b == null) return null;
            if (a == null) return ConvertInput(b, options); // Convert result for consistency
            if (b == null) return ConvertInput(a, options); // Convert result for consistency

            object processedA = ConvertInput(a, options);
            object processedB = ConvertInput(b, options);

            // Promote to highest precision before comparison
            TypeCode typeCode = ConvertToHighestPrecision(ref processedA, ref processedB, options);

            // Use pattern matching for clarity
            return (processedA, processedB) switch
            {
                (byte valA, byte valB) => Math.Max(valA, valB),
                (sbyte valA, sbyte valB) => Math.Max(valA, valB),
                (short valA, short valB) => Math.Max(valA, valB),
                (ushort valA, ushort valB) => Math.Max(valA, valB),
                (int valA, int valB) => Math.Max(valA, valB),
                (uint valA, uint valB) => Math.Max(valA, valB),
                (long valA, long valB) => Math.Max(valA, valB),
                (ulong valA, ulong valB) => Math.Max(valA, valB),
                (float valA, float valB) => Math.Max(valA, valB),
                (double valA, double valB) => Math.Max(valA, valB),
                (decimal valA, decimal valB) => Math.Max(valA, valB),
                 // Add BigInteger/BigDecimal if needed
                _ => throw new InvalidOperationException($"Cannot compare types {processedA?.GetType().Name} and {processedB?.GetType().Name}")
            };
        }

        public static object? Min(object? a, object? b) => Min(a, b, new MathHelperOptions());

        public static object? Min(object? a, object? b, MathHelperOptions options)
        {
            if (a == null && b == null) return null;
            if (a == null) return ConvertInput(b, options);
            if (b == null) return ConvertInput(a, options);

            object processedA = ConvertInput(a, options);
            object processedB = ConvertInput(b, options);

            TypeCode typeCode = ConvertToHighestPrecision(ref processedA, ref processedB, options);

            return (processedA, processedB) switch
            {
                (byte valA, byte valB) => Math.Min(valA, valB),
                (sbyte valA, sbyte valB) => Math.Min(valA, valB),
                (short valA, short valB) => Math.Min(valA, valB),
                (ushort valA, ushort valB) => Math.Min(valA, valB),
                (int valA, int valB) => Math.Min(valA, valB),
                (uint valA, uint valB) => Math.Min(valA, valB),
                (long valA, long valB) => Math.Min(valA, valB),
                (ulong valA, ulong valB) => Math.Min(valA, valB),
                (float valA, float valB) => Math.Min(valA, valB),
                (double valA, double valB) => Math.Min(valA, valB),
                (decimal valA, decimal valB) => Math.Min(valA, valB),
                // Add BigInteger/BigDecimal if needed
                 _ => throw new InvalidOperationException($"Cannot compare types {processedA?.GetType().Name} and {processedB?.GetType().Name}")
            };
        }

        // --- Type Promotion Logic (Modified slightly to handle nulls better) ---
        private static TypeCode ConvertToHighestPrecision(ref object a, ref object b, MathHelperOptions options)
        {
            // Assumes a and b are not null and are already processed numeric types
            TypeCode typeCodeA = Type.GetTypeCode(a.GetType());
            TypeCode typeCodeB = Type.GetTypeCode(b.GetType());

            if (typeCodeA == typeCodeB) return typeCodeA;

            // Handle BigInteger/BigDecimal promotion if they were supported inputs
            // if (a is BigInteger || b is BigInteger) { /* promote to BigInteger or BigDecimal */ }
            // if (a is BigDecimal || b is BigDecimal) { /* promote to BigDecimal */ }

            // Decimal has highest precision among standard types
            if (typeCodeA == TypeCode.Decimal) { b = ConvertToDecimal(b, options); return TypeCode.Decimal; }
            if (typeCodeB == TypeCode.Decimal) { a = ConvertToDecimal(a, options); return TypeCode.Decimal; }

            // Double is next
            if (typeCodeA == TypeCode.Double) { b = ConvertToDouble(b, options); return TypeCode.Double; }
            if (typeCodeB == TypeCode.Double) { a = ConvertToDouble(a, options); return TypeCode.Double; }

            // Float is next
            if (typeCodeA == TypeCode.Single) { b = ConvertToSingle(b, options); return TypeCode.Single; }
            if (typeCodeB == TypeCode.Single) { a = ConvertToSingle(a, options); return TypeCode.Single; }

            // Unsigned long
            if (typeCodeA == TypeCode.UInt64) { b = ConvertToULong(b, options); return TypeCode.UInt64; }
            if (typeCodeB == TypeCode.UInt64) { a = ConvertToULong(a, options); return TypeCode.UInt64; }

            // Signed long
            if (typeCodeA == TypeCode.Int64) { b = ConvertToLong(b, options); return TypeCode.Int64; }
            if (typeCodeB == TypeCode.Int64) { a = ConvertToLong(a, options); return TypeCode.Int64; }

            // Follow implicit C# promotion rules (roughly)
            // Promote smaller integers to int/uint/long/ulong as appropriate
            // This part gets complex if mixing signed/unsigned of same size.
            // For Min/Max, promoting to the type with larger range usually works.
            // Let's simplify: promote to Int64 if signed types involved, UInt64 if only unsigned.
             if (IsSignedInteger(typeCodeA) || IsSignedInteger(typeCodeB))
            {
                // Promote both to Int64 if possible, otherwise handle incompatibility
                try { a = ConvertToLong(a, options); } catch { /* Handle cases like ulong -> long */ throw new InvalidOperationException($"Cannot implicitly convert {a.GetType().Name} to long for comparison."); }
                try { b = ConvertToLong(b, options); } catch { throw new InvalidOperationException($"Cannot implicitly convert {b.GetType().Name} to long for comparison."); }
                return TypeCode.Int64;
            }
            else if (IsUnsignedInteger(typeCodeA) || IsUnsignedInteger(typeCodeB))
            {
                 // Promote both to UInt64
                try { a = ConvertToULong(a, options); } catch { throw new InvalidOperationException($"Cannot implicitly convert {a.GetType().Name} to ulong for comparison."); }
                try { b = ConvertToULong(b, options); } catch { throw new InvalidOperationException($"Cannot implicitly convert {b.GetType().Name} to ulong for comparison."); }
                return TypeCode.UInt64;
            }

            // Fallback or error
             throw new InvalidOperationException($"Cannot determine common type for {a.GetType().Name} and {b.GetType().Name}");
        }

        private static bool IsSignedInteger(TypeCode code) => code is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;
        private static bool IsUnsignedInteger(TypeCode code) => code is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Char; // Char is technically unsigned

        // --- Single Operand Functions remain largely the same ---
        // (Just ensure they use ConvertInput first)

        public static object Abs(object? a, MathHelperOptions options)
        {
            if (a == null) return null!; // Or throw? Or return 0? Consistent handling needed. Let's return null for now.
            object processedA = ConvertInput(a, options);
            if (options.DecimalAsDefault) return Math.Abs(ConvertToDecimal(processedA, options));
            return Math.Abs(ConvertToDouble(processedA, options));
        }

        public static object Acos(object? a, MathHelperOptions options) => Math.Acos(ConvertToDouble(ConvertInput(a, options), options));
        public static object Asin(object? a, MathHelperOptions options) => Math.Asin(ConvertToDouble(ConvertInput(a, options), options));
        public static object Atan(object? a, MathHelperOptions options) => Math.Atan(ConvertToDouble(ConvertInput(a, options), options));

        public static object Atan2(object? a, object? b, MathHelperOptions options) =>
            Math.Atan2(ConvertToDouble(ConvertInput(a, options), options), ConvertToDouble(ConvertInput(b, options), options));

        public static object Ceiling(object? a, MathHelperOptions options)
        {
             if (a == null) return null!;
             object processedA = ConvertInput(a, options);
             if (options.DecimalAsDefault) return Math.Ceiling(ConvertToDecimal(processedA, options));
             return Math.Ceiling(ConvertToDouble(processedA, options));
        }

        public static object Cos(object? a, MathHelperOptions options) => Math.Cos(ConvertToDouble(ConvertInput(a, options), options));
        public static object Exp(object? a, MathHelperOptions options) => Math.Exp(ConvertToDouble(ConvertInput(a, options), options));

        public static object Floor(object? a, MathHelperOptions options)
        {
            if (a == null) return null!;
            object processedA = ConvertInput(a, options);
            if (options.DecimalAsDefault) return Math.Floor(ConvertToDecimal(processedA, options));
            return Math.Floor(ConvertToDouble(processedA, options));
        }

        public static object IEEERemainder(object? a, object? b, MathHelperOptions options) =>
            Math.IEEERemainder(ConvertToDouble(ConvertInput(a, options), options), ConvertToDouble(ConvertInput(b, options), options));

        public static object Ln(object? a, MathHelperOptions options) => Math.Log(ConvertToDouble(ConvertInput(a, options), options)); // Natural log

        public static object Log(object? a, object? b, MathHelperOptions options) =>
             Math.Log(ConvertToDouble(ConvertInput(a, options), options), ConvertToDouble(ConvertInput(b, options), options)); // Log base b

        public static object Log10(object? a, MathHelperOptions options) => Math.Log10(ConvertToDouble(ConvertInput(a, options), options));

        public static object Pow(object? a, object? b, MathHelperOptions options)
        {
            // Keep BigInteger/BigDecimal for Pow as it handles large exponents correctly
            object processedA = ConvertInput(a, options);
            object processedB = ConvertInput(b, options);

            // If DecimalAsDefault, try using BigDecimal for potentially higher precision power
            if (options.DecimalAsDefault)
            {
                try
                {
                    // BigDecimal Pow often expects an integer exponent. Handle fractional exponents via double.
                    // This implementation assumes integer exponent for BigDecimal. Adjust if needed.
                    var decA = ConvertToDecimal(processedA, options);
                    var decB = ConvertToDecimal(processedB, options);

                    // Check if exponent is effectively an integer
                    if (decB == Math.Truncate(decB))
                    {
                        // Need ExtendedNumerics.BigDecimal for Pow
                        var bigDecBase = new BigDecimal(decA);
                        // BigInteger constructor might throw if decB is too large or fractional
                        var bigIntExponent = new BigInteger(decB);
                        return (decimal)BigDecimal.Pow(bigDecBase, bigIntExponent);
                    }
                    // Fallback to double for non-integer exponents
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
                {
                    // Fallback to double if conversion fails or exponent isn't suitable for BigInteger
                }
            }

            // Default or fallback: Use Math.Pow (double)
            return Math.Pow(ConvertToDouble(processedA, options), ConvertToDouble(processedB, options));
        }


        public static object Round(object? a, object? b, MidpointRounding rounding, MathHelperOptions options)
        {
            if (a == null || b == null) return null!; // Or throw?
            object processedA = ConvertInput(a, options);
            object processedB = ConvertInput(b, options); // Digits argument

            int digits = ConvertToInt(processedB, options);

            if (options.DecimalAsDefault)
            {
                return Math.Round(ConvertToDecimal(processedA, options), digits, rounding);
            }
            else
            {
                return Math.Round(ConvertToDouble(processedA, options), digits, rounding);
            }
        }

        public static object Sign(object? a, MathHelperOptions options)
        {
             if (a == null) return null!;
             object processedA = ConvertInput(a, options);
             if (options.DecimalAsDefault) return Math.Sign(ConvertToDecimal(processedA, options));
             // Math.Sign has overloads for double, decimal, etc. but returns int
             // We might want to match the input type's Sign behavior if preserving type is important
             // For now, stick to Math.Sign which returns int.
             return Math.Sign(ConvertToDouble(processedA, options)); // Or other overloads based on type
        }

        public static object Sin(object? a, MathHelperOptions options) => Math.Sin(ConvertToDouble(ConvertInput(a, options), options));
        public static object Sqrt(object? a, MathHelperOptions options) => Math.Sqrt(ConvertToDouble(ConvertInput(a, options), options));
        public static object Tan(object? a, MathHelperOptions options) => Math.Tan(ConvertToDouble(ConvertInput(a, options), options));

        public static object Truncate(object? a, MathHelperOptions options)
        {
            if (a == null) return null!;
            object processedA = ConvertInput(a, options);
            if (options.DecimalAsDefault) return Math.Truncate(ConvertToDecimal(processedA, options));
            return Math.Truncate(ConvertToDouble(processedA, options));
        }

        // --- Conversion Helpers ---

        /// <summary>
        /// Converts input value (string, bool, char) to a base numeric type (double or decimal).
        /// Returns other numeric types directly.
        /// </summary>
        private static object ConvertInput(object? value, MathHelperOptions options)
        {
             if (value == null) throw new ArgumentNullException(nameof(value)); // Or handle differently?

            return value switch
            {
                // Prioritize target type based on options
                string s => options.DecimalAsDefault
                    ? decimal.Parse(s, NumberStyles.Any, options.CultureInfo)
                    : double.Parse(s, NumberStyles.Any, options.CultureInfo),

                char c when options.AllowCharValues => options.DecimalAsDefault
                    ? (decimal)c // Implicit char to numeric
                    : (double)c, // Implicit char to numeric
                char c when !options.AllowCharValues => options.DecimalAsDefault // Treat as string if not allowing direct char value
                    ? decimal.Parse(c.ToString(), NumberStyles.Any, options.CultureInfo)
                    : double.Parse(c.ToString(), NumberStyles.Any, options.CultureInfo),

                bool b when options.AllowBooleanCalculation => options.DecimalAsDefault ? (b ? 1M : 0M) : (b ? 1d : 0d),
                bool b when !options.AllowBooleanCalculation => throw new InvalidOperationException("Boolean values are not allowed in calculations."),

                // Pass through existing numeric types
                byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => value,
                // BigInteger / BigDecimal would be passed through here if supported input

                _ => throw new InvalidOperationException($"Cannot convert type {value.GetType().Name} to a numeric value for calculation.")
            };
        }


        private static double ConvertToDouble(object? value, MathHelperOptions options)
        {
            if (value is double d) return d;
            if (value == null) throw new ArgumentNullException(nameof(value)); // Or return 0? Consistent handling.
            // Ensure ConvertInput was called first, so value should be numeric here
             try { return Convert.ToDouble(value, options.CultureInfo); }
             catch (Exception ex) { throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Double.", ex); }
        }

        private static decimal ConvertToDecimal(object? value, MathHelperOptions options)
        {
            if (value is decimal m) return m;
            if (value == null) throw new ArgumentNullException(nameof(value));
             try { return Convert.ToDecimal(value, options.CultureInfo); }
             catch (Exception ex) { throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Decimal.", ex); }
        }

        private static float ConvertToSingle(object? value, MathHelperOptions options)
        {
             if (value is float f) return f;
             if (value == null) throw new ArgumentNullException(nameof(value));
             try { return Convert.ToSingle(value, options.CultureInfo); }
             catch (Exception ex) { throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Single.", ex); }
        }

        private static long ConvertToLong(object? value, MathHelperOptions options)
        {
             if (value is long l) return l;
             if (value == null) throw new ArgumentNullException(nameof(value));
             try { return Convert.ToInt64(value, options.CultureInfo); }
             catch (Exception ex) { throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Int64.", ex); }
        }

        private static ulong ConvertToULong(object? value, MathHelperOptions options)
        {
             if (value is ulong ul) return ul;
             if (value == null) throw new ArgumentNullException(nameof(value));
             try { return Convert.ToUInt64(value, options.CultureInfo); }
             catch (Exception ex) { throw new InvalidCastException($"Cannot convert {value.GetType().Name} to UInt64.", ex); }
        }

        private static int ConvertToInt(object? value, MathHelperOptions options)
        {
            if (value is int i) return i;
            if (value == null) throw new ArgumentNullException(nameof(value));
             try { return Convert.ToInt32(value, options.CultureInfo); }
             catch (Exception ex) { throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Int32.", ex); }
        }

        /// <summary>
        /// Checks for Infinity results when operating on doubles/floats with overflow protection.
        /// </summary>
        private static void CheckFloatingPointOverflow(object value)
        {
            switch (value)
            {
                case double d when double.IsInfinity(d):
                case float f when float.IsInfinity(f):
                    throw new OverflowException("Arithmetic operation resulted in an overflow (Infinity).");
            }
            // No need to check for NaN as it's not typically considered an overflow
        }

        // --- Remove obsolete dynamic helpers and Execute* methods ---
        // Remove AddFunc, SubtractFunc, MultiplyFunc, DivideFunc, ModuloFunc and Checked versions
        // Remove ExecuteOperation(a,b, operatorName, func)
        // Remove ExecuteByteOperation, ExecuteCharOperation, ..., ExecuteDecimalOperation
        // Remove TypeCodeBitSize (replaced by promotion logic in ConvertToHighestPrecision)
        // Remove CheckOverflow(dynamic value) - replaced by CheckFloatingPointOverflow(object value)

    }
}
