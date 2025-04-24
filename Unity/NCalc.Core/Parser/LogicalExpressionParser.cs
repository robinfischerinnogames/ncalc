using System;
using System.Collections.Generic;
using ExtendedNumerics;
using NCalc.Domain;
using NCalc.Exceptions;
using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using Identifier = NCalc.Domain.Identifier;

namespace NCalc.Parser
{
    /// <summary>
    /// Class responsible for parsing strings into <see cref="LogicalExpression" /> objects.
    /// </summary>
    public static class LogicalExpressionParser
    {
        private const string InvalidTokenMessage = "Invalid token in expression";
        private static readonly Parser<LogicalExpression> Parser;

        private static readonly ValueExpression True = new(true);
        private static readonly ValueExpression False = new(false);

        static LogicalExpressionParser()
        {
            /*
             * Grammar:
             * expression     => ternary ( ( "-" | "+" ) ternary )* ;
             * ternary        => logical ( "?" logical ":" logical)?
             * logical        => equality ( ( "and" | "or" ) equality )* ;
             * equality       => relational ( ( "=" | "!=" | ... ) relational )* ;
             * relational     => shift ( ( ">=" | ">" | ... ) shift )* ;
             * shift          => additive ( ( "<<" | ">>" ) additive )* ;
             * additive       => multiplicative ( ( "-" | "+" ) multiplicative )* ;
             * multiplicative => exponential ( "/" | "*" | "%") exponential )* ;
             * exponential    => unary ( "**" ) unary )* ;
             * unary          => ( "-" | "not" | "!" ) primary
             *
             * primary        => NUMBER
             *                  | STRING
             *                  | "true"
             *                  | "false"
             *                  | ("[" | "{") anything ("]" | "}")
             *                  | function
             *                  | list
             *                  | "(" expression ")" ;
             *
             * function       => Identifier "(" arguments ")"
             * arguments      => expression ( ("," | ";") expression )*
             */
            // The Deferred helper creates a parser that can be referenced by others before it is defined
            Deferred<LogicalExpression> expression = Deferred<LogicalExpression>();

            Parser<long?> exponentNumberPart =
                Literals.Text("e", true).SkipAnd(Literals.Integer())
                    .ThenElse<long?>(x => x, null);

            // [integral_value]['.'decimal_value}]['e'exponent_value]
            Parser<LogicalExpression> number =
                SkipWhiteSpace(OneOf(
                        Literals.Char('.')
                            .SkipAnd(ZeroOrMany(Literals.Char('0')).ThenElse(x => x.Count, 0)
                                .And(ZeroOrOne(Literals.Integer(NumberOptions.None)).ThenElse<long?>(x => x, 0))
                                .Then(x =>
                                {
                                    if (x is { Item1: 0, Item2: 0 })
                                    {
                                        throw new NCalcParserException(InvalidTokenMessage);
                                    }

                                    return (x.Item1, x.Item2);
                                }))
                            .And(exponentNumberPart)
                            .Then(x => (0L, x.Item1.Item1, x.Item1.Item2, x.Item2)),
                        Literals.Text("0x")
                            .SkipAnd(Terms.Pattern(c => "0123456789abcdefABCDEF".Contains(c.ToString())))
                            .Then(x => Convert.ToInt64(x.ToString(), 16))
                            .Then<(long, int, long?, long?)>(x => (x, 0, null, null)),
                        Literals.Text("0b")
                            .SkipAnd(Terms.Pattern(c => c == '0' || c == '1'))
                            .Then(x => Convert.ToInt64(x.ToString(), 2))
                            .Then<(long, int, long?, long?)>(x => (x, 0, null, null)),
                        Literals.Text("0o")
                            .SkipAnd(Terms.Pattern(c => "01234567".Contains(c.ToString())))
                            .Then(x => Convert.ToInt64(x.ToString(), 8))
                            .Then<(long, int, long?, long?)>(x => (x, 0, null, null)),
                        Literals.Integer()
                            .And(Literals.Char('.')
                                .SkipAnd(ZeroOrMany(Literals.Char('0')).ThenElse(x => x.Count, 0))
                                .And(ZeroOrOne(Literals.Integer(NumberOptions.None)))
                                .ThenElse<(int, long?)>(x => (x.Item1, x.Item2), (0, null)))
                            .And(exponentNumberPart)
                            .Then(x => (x.Item1, x.Item2.Item1, x.Item2.Item2, x.Item3))
                    ))
                    .Then(ParseNumber);

            Parser<char> comma = Terms.Char(',');
            Parser<string> divided = Terms.Text("/");
            Parser<string> times = Terms.Text("*");
            Parser<string> modulo = Terms.Text("%");
            Parser<string> minus = Terms.Text("-");
            Parser<string> plus = Terms.Text("+");

            Parser<string> equal = OneOf(Terms.Text("=="), Terms.Text("="));
            Parser<string> notEqual = OneOf(Terms.Text("<>"), Terms.Text("!="));
            Parser<string> @in = Terms.Text("in", true);
            Parser<string> notIn = Terms.Text("not in", true);

            Parser<string> like = Terms.Text("like", true);
            Parser<string> notLike = Terms.Text("not like", true);

            Parser<string> greater = Terms.Text(">");
            Parser<string> greaterOrEqual = Terms.Text(">=");
            Parser<string> lesser = Terms.Text("<");
            Parser<string> lesserOrEqual = Terms.Text("<=");

            Parser<string> leftShift = Terms.Text("<<");
            Parser<string> rightShift = Terms.Text(">>");

            Parser<string> exponent = Terms.Text("**");
            Parser<char> openParen = Terms.Char('(');
            Parser<char> closeParen = Terms.Char(')');
            Parser<char> openBrace = Terms.Char('[');
            Parser<char> closeBrace = Terms.Char(']');
            Parser<char> openCurlyBrace = Terms.Char('{');
            Parser<char> closeCurlyBrace = Terms.Char('}');
            Parser<char> questionMark = Terms.Char('?');
            Parser<char> colon = Terms.Char(':');
            Parser<char> semicolon = Terms.Char(';');

            Parser<TextSpan> identifier = Terms.Identifier();

            Parser<string> not = OneOf(
                Terms.Text("NOT", true).AndSkip(OneOf(Literals.WhiteSpace().Or(Not(AnyCharBefore(openParen))))),
                Terms.Text("!"));
            Parser<string> and = OneOf(Terms.Text("AND", true), Terms.Text("&&"));
            Parser<string> or = OneOf(Terms.Text("OR", true), Terms.Text("||"));

            Parser<string> bitwiseAnd = Terms.Text("&");
            Parser<string> bitwiseOr = Terms.Text("|");
            Parser<string> bitwiseXOr = Terms.Text("^");
            Parser<string> bitwiseNot = Terms.Text("~");

            // "(" expression ")"
            Parser<LogicalExpression> groupExpression = Between(openParen, expression, closeParen);

            SequenceSkipAnd<char, TextSpan> braceIdentifier = openBrace
                .SkipAnd(AnyCharBefore(closeBrace, consumeDelimiter: true, failOnEof: true)
                    .ElseError("Brace not closed."));

            SequenceSkipAnd<char, TextSpan> curlyBraceIdentifier =
                openCurlyBrace.SkipAnd(AnyCharBefore(closeCurlyBrace, consumeDelimiter: true, failOnEof: true)
                    .ElseError("Brace not closed."));

            // ("[" | "{") identifier ("]" | "}")
            Parser<LogicalExpression> identifierExpression = OneOf(
                    braceIdentifier,
                    curlyBraceIdentifier,
                    identifier)
                .Then<LogicalExpression>(x => new Identifier(x.ToString()!));

            // list => "(" (expression ("," expression)*)? ")"
            Parser<LogicalExpression> populatedList =
                Between(openParen, Separated(comma.Or(semicolon), expression),
                        closeParen.ElseError("Parenthesis not closed."))
                    .Then<LogicalExpression>(values => new LogicalExpressionList(values));

            Parser<LogicalExpression> emptyList =
                openParen.AndSkip(closeParen).Then<LogicalExpression>(_ => new LogicalExpressionList());

            Parser<LogicalExpression> list = OneOf(emptyList, populatedList);

            Parser<LogicalExpression> function = identifier
                .And(list)
                .Then<LogicalExpression>(static x =>
                    new Function(new Identifier(x.Item1.ToString()!), (LogicalExpressionList)x.Item2));

            Parser<LogicalExpression> booleanTrue = Terms.Text("true", true)
                .Then<LogicalExpression>(True);
            Parser<LogicalExpression> booleanFalse = Terms.Text("false", true)
                .Then<LogicalExpression>(False);

            Parser<LogicalExpression> singleQuotesStringValue =
                Terms.String(StringLiteralQuotes.Single)
                    .Then<LogicalExpression>(static (ctx, value) =>
                    {
                        if (value.Length == 1 &&
                            ((LogicalExpressionParserContext)ctx).Options.HasFlag(ExpressionOptions.AllowCharValues))
                        {
                            return new ValueExpression(value.Span[0]);
                        }

                        return new ValueExpression(value.ToString());
                    });

            Parser<LogicalExpression> doubleQuotesStringValue =
                Terms
                    .String(StringLiteralQuotes.Double)
                    .Then<LogicalExpression>(value => new ValueExpression(value.ToString()!));

            Parser<LogicalExpression> stringValue = OneOf(singleQuotesStringValue, doubleQuotesStringValue);

            Parser<TextSpan> charIsNumber = Literals.Pattern(char.IsNumber);

            Sequence<TextSpan, TextSpan, TextSpan> dateDefinition = charIsNumber
                .AndSkip(divided)
                .And(charIsNumber)
                .AndSkip(divided)
                .And(charIsNumber);

            // date => number/number/number
            Parser<LogicalExpression> date = dateDefinition.Then<LogicalExpression>(static date =>
            {
                if (DateTime.TryParse($"{date.Item1}/{date.Item2}/{date.Item3}", out DateTime result))
                {
                    return new ValueExpression(result);
                }

                throw new FormatException("Invalid DateTime format.");
            });

            // time => number:number:number
            Sequence<TextSpan, TextSpan, TextSpan> timeDefinition = charIsNumber
                .AndSkip(colon)
                .And(charIsNumber)
                .AndSkip(colon)
                .And(charIsNumber);

            Parser<LogicalExpression> time = timeDefinition.Then<LogicalExpression>(static time =>
            {
                if (TimeSpan.TryParse($"{time.Item1}:{time.Item2}:{time.Item3}", out TimeSpan result))
                {
                    return new ValueExpression(result);
                }

                throw new FormatException("Invalid TimeSpan format.");
            });

            // dateAndTime => number/number/number number:number:number
            Parser<LogicalExpression> dateAndTime = dateDefinition.AndSkip(Literals.WhiteSpace()).And(timeDefinition)
                .Then<LogicalExpression>(
                    static dateTime =>
                    {
                        if (DateTime.TryParse(
                                $"{dateTime.Item1}/{dateTime.Item2}/{dateTime.Item3} {dateTime.Item4.Item1}:{dateTime.Item4.Item2}:{dateTime.Item4.Item3}",
                                out DateTime result))
                        {
                            return new ValueExpression(result);
                        }

                        throw new FormatException("Invalid DateTime format.");
                    });

            // datetime => '#' dateAndTime | date | time  '#';
            SequenceAndSkip<LogicalExpression, char> dateTime = Terms
                .Char('#')
                .SkipAnd(OneOf(dateAndTime, date, time))
                .AndSkip(Literals.Char('#'));

            Parser<LogicalExpression> decimalNumber = Terms.Number<decimal>()
                .Then<LogicalExpression>(static d => new ValueExpression(d));
            Parser<LogicalExpression> doubleNumber = Terms.Number<double>(NumberOptions.Float)
                .Then<LogicalExpression>(static d => new ValueExpression(d));

            Parser<LogicalExpression> decimalOrDoubleNumber = OneOf(decimalNumber, doubleNumber);

            Func<char, bool> isHexDigit = Character.IsHexDigit;

            Parser<TextSpan> eightHexSequence = Terms
                .Pattern(isHexDigit, 8, 8);

            Parser<TextSpan> fourHexSequence = Terms
                .Pattern(isHexDigit, 4, 4);

            Parser<TextSpan> twelveHexSequence = Terms
                .Pattern(isHexDigit, 12, 12);

            Parser<TextSpan> thirtyTwoHexSequence = Terms
                .Pattern(isHexDigit, 32, 32);

            Parser<LogicalExpression> guidWithHyphens = eightHexSequence
                .AndSkip(minus)
                .And(fourHexSequence)
                .AndSkip(minus)
                .And(fourHexSequence)
                .AndSkip(minus)
                .And(fourHexSequence)
                .AndSkip(minus)
                .And(twelveHexSequence)
                .Then<LogicalExpression>(static g =>
                    new ValueExpression(Guid.Parse(g.Item1.ToString() + g.Item2 + g.Item3 + g.Item4 + g.Item5)));

            Parser<LogicalExpression> guidWithoutHyphens = thirtyTwoHexSequence
                .AndSkip(Not(decimalOrDoubleNumber))
                .Then<LogicalExpression>(static g => new ValueExpression(Guid.Parse(g.ToString()!)));

            Parser<LogicalExpression> guid = OneOf(guidWithHyphens, guidWithoutHyphens);

            // primary => GUID | NUMBER | identifier| DateTime | string | function | boolean | groupExpression | list ;
            Parser<LogicalExpression> primary = OneOf(
                guid,
                number,
                decimalOrDoubleNumber,
                booleanTrue,
                booleanFalse,
                dateTime,
                stringValue,
                function,
                groupExpression,
                identifierExpression,
                list);

            // exponential => unary ( "**" unary )* ;
            Parser<LogicalExpression> exponential = primary.And(ZeroOrMany(exponent.And(primary)))
                .Then(static x =>
                {
                    LogicalExpression result = null!;

                    switch (x.Item2.Count)
                    {
                        case 0:
                            result = x.Item1;
                            break;
                        case 1:
                            result = new BinaryExpression(BinaryExpressionType.Exponentiation, x.Item1,
                                x.Item2[0].Item2);
                            break;
                        default:
                        {
                            for (int i = x.Item2.Count - 1; i > 0; i--)
                            {
                                result = new BinaryExpression(BinaryExpressionType.Exponentiation, x.Item2[i - 1].Item2,
                                    x.Item2[i].Item2);
                            }

                            result = new BinaryExpression(BinaryExpressionType.Exponentiation, x.Item1, result);
                            break;
                        }
                    }

                    return result;
                });

            // ( "-" | "not" ) unary | primary;
            Parser<LogicalExpression> unary = exponential.Unary(
                (not, static value => new UnaryExpression(UnaryExpressionType.Not, value)),
                (minus, static value => new UnaryExpression(UnaryExpressionType.Negate, value)),
                (bitwiseNot, static value => new UnaryExpression(UnaryExpressionType.BitwiseNot, value))
            );

            // multiplicative => unary ( ( "/" | "*" | "%" ) unary )* ;
            Parser<LogicalExpression> multiplicative = unary.LeftAssociative(
                (divided, static (a, b) => new BinaryExpression(BinaryExpressionType.Div, a, b)),
                (times, static (a, b) => new BinaryExpression(BinaryExpressionType.Times, a, b)),
                (modulo, static (a, b) => new BinaryExpression(BinaryExpressionType.Modulo, a, b))
            );

            // additive => multiplicative ( ( "-" | "+" ) multiplicative )* ;
            Parser<LogicalExpression> additive = multiplicative.LeftAssociative(
                (plus, static (a, b) => new BinaryExpression(BinaryExpressionType.Plus, a, b)),
                (minus, static (a, b) => new BinaryExpression(BinaryExpressionType.Minus, a, b))
            );

            // shift => additive ( ( "<<" | ">>" ) additive )* ;
            Parser<LogicalExpression> shift = additive.LeftAssociative(
                (leftShift, static (a, b) => new BinaryExpression(BinaryExpressionType.LeftShift, a, b)),
                (rightShift, static (a, b) => new BinaryExpression(BinaryExpressionType.RightShift, a, b))
            );

            // relational => shift ( ( ">=" | "<=" | "<" | ">" | "in" | "not in" ) shift )* ;
            Parser<LogicalExpression> relational = shift.And(ZeroOrMany(OneOf(
                        greaterOrEqual.Then(BinaryExpressionType.GreaterOrEqual),
                        lesserOrEqual.Then(BinaryExpressionType.LesserOrEqual),
                        lesser.Then(BinaryExpressionType.Lesser),
                        greater.Then(BinaryExpressionType.Greater),
                        @in.Then(BinaryExpressionType.In),
                        notIn.Then(BinaryExpressionType.NotIn),
                        like.Then(BinaryExpressionType.Like),
                        notLike.Then(BinaryExpressionType.NotLike)
                    )
                    .And(shift)))
                .Then(ParseBinaryExpression);

            Parser<LogicalExpression> equality = relational.And(ZeroOrMany(OneOf(
                        equal.Then(BinaryExpressionType.Equal),
                        notEqual.Then(BinaryExpressionType.NotEqual))
                    .And(relational)))
                .Then(ParseBinaryExpression);

            Parser<BinaryExpressionType> andTypeParser = and.Then(BinaryExpressionType.And)
                .Or(bitwiseAnd.Then(BinaryExpressionType.BitwiseAnd));

            Parser<BinaryExpressionType> orTypeParser = or.Then(BinaryExpressionType.Or)
                .Or(bitwiseOr.Then(BinaryExpressionType.BitwiseOr));

            Parser<BinaryExpressionType> xorTypeParser = bitwiseXOr.Then(BinaryExpressionType.BitwiseXOr);

            // "and" has higher precedence than "or"
            Parser<LogicalExpression> andParser = equality.And(ZeroOrMany(andTypeParser.And(equality)))
                .Then(ParseBinaryExpression);

            Parser<LogicalExpression> orParser = andParser.And(ZeroOrMany(orTypeParser.And(andParser)))
                .Then(ParseBinaryExpression);

            // logical => equality ( ( "and" | "or" | "xor" ) equality )* ;
            Parser<LogicalExpression> logical = orParser.And(ZeroOrMany(xorTypeParser.And(orParser)))
                .Then(ParseBinaryExpression);

            // ternary => logical("?" logical ":" logical) ?
            Parser<LogicalExpression> ternary = logical
                .And(ZeroOrOne(questionMark.SkipAnd(logical).AndSkip(colon).And(logical)))
                .Then(static x => x.Item2.Item1 == null
                    ? x.Item1
                    : new TernaryExpression(x.Item1, x.Item2.Item1, x.Item2.Item2))
                .Or(logical);

            Parser<LogicalExpression> operatorSequence = ternary.LeftAssociative(
                (OneOrMany(OneOf(
                        divided, times, modulo, plus,
                        minus, leftShift, rightShift, greaterOrEqual,
                        lesserOrEqual, greater, lesser, equal,
                        notEqual)),
                    static (_, _) => throw new InvalidOperationException("Unknown operator sequence.")));

            expression.Parser = operatorSequence;
            Parser<LogicalExpression> expressionParser = expression.AndSkip(ZeroOrMany(Literals.WhiteSpace(true))).Eof()
                .ElseError(InvalidTokenMessage);

            AppContext.TryGetSwitch("NCalc.EnableParlotParserCompilation", out bool enableParserCompilation);

            Parser = enableParserCompilation ? expressionParser.Compile() : expressionParser;
        }

        private static LogicalExpression ParseNumber(ParseContext context, (long, int, long?, long?) number)
        {
            long integralValue = number.Item1;
            int zeroCount = number.Item2;
            long? decimalPart = number.Item3;
            long? exponentPart = number.Item4;

            if (((LogicalExpressionParserContext)context).Options.HasFlag(ExpressionOptions.DecimalAsDefault))
            {
                decimal result1 = integralValue;

                // decimal part?
                if (decimalPart != null && decimalPart.Value != 0)
                {
                    double digits = Math.Floor(Math.Log10(decimalPart.Value) + 1) + zeroCount;
                    result1 += decimalPart.Value / (decimal)Math.Pow(10, digits);
                }

                // exponent part?
                if (exponentPart == null)
                {
                    return new ValueExpression(result1);
                }

                var left = new BigDecimal(result1);
                BigDecimal right = BigDecimal.Pow(10, exponentPart.Value);

                BigDecimal res = BigDecimal.Multiply(left, right);

                if (res > decimal.MaxValue)
                    // There is no decimal.PositiveInfinity
                {
                    return new ValueExpression(double.PositiveInfinity);
                }

                if (res < decimal.MinValue)
                    // There is no decimal.NegativeInfinity
                {
                    return new ValueExpression(double.NegativeInfinity);
                }

                result1 = (decimal)res;

                return new ValueExpression(result1);
            }

            double result = integralValue;

            // decimal part?
            if (decimalPart != null && decimalPart.Value != 0)
            {
                double digits = Math.Floor(Math.Log10(decimalPart.Value) + 1) + zeroCount;
                result += decimalPart.Value / Math.Pow(10, digits);
            }

            // exponent part?
            if (exponentPart != null)
            {
                BigDecimal left = BigDecimal.Parse(result);
                BigDecimal right = BigDecimal.Pow(10, exponentPart.Value);

                BigDecimal res = BigDecimal.Multiply(left, right);

                if (res > double.MaxValue)
                {
                    result = double.PositiveInfinity;
                }
                else if (res < double.MinValue)
                {
                    result = double.NegativeInfinity;
                }
                else
                {
                    result = (double)res;
                }
            }

            if (decimalPart != null || exponentPart != null)
            {
                return new ValueExpression(result);
            }

            return new ValueExpression(integralValue);
        }

        private static LogicalExpression ParseBinaryExpression(
            (LogicalExpression, IReadOnlyList<(BinaryExpressionType, LogicalExpression)>) x)
        {
            LogicalExpression? result = x.Item1;

            foreach ((BinaryExpressionType, LogicalExpression) op in x.Item2)
            {
                result = new BinaryExpression(op.Item1, result, op.Item2);
            }

            return result;
        }

        public static LogicalExpression Parse(LogicalExpressionParserContext context)
        {
            if (Parser.TryParse(context, out LogicalExpression? result, out ParseError? error))
            {
                return result;
            }

            string message;
            if (error != null)
            {
                message = $"{error.Message} at position {error.Position}";
            }
            else
            {
                message = $"Error parsing the expression at position {context.Scanner.Cursor.Position}";
            }

            throw new NCalcParserException(message);
        }
    }
}