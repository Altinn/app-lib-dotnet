using System.Globalization;
using System.Text.RegularExpressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;

namespace Altinn.App.Core.Internal.Expressions;

/// <summary>
/// Static class used to evaluate expressions. Holds the implementation for all expression functions.
/// </summary>
public static class ExpressionEvaluator
{
    /// <summary>
    /// Shortcut for evaluating a boolean expression on a given property on a <see cref="BaseComponent" />
    /// </summary>
    public static async Task<bool> EvaluateBooleanExpression(
        LayoutEvaluatorState state,
        ComponentContext context,
        string property,
        bool defaultReturn
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(context.Component);
            var expr = property switch
            {
                "hidden" => context.Component.Hidden,
                "required" => context.Component.Required,
                _ => throw new ExpressionEvaluatorTypeErrorException($"unknown boolean expression property {property}"),
            };

            return await EvaluateExpression(state, expr, context) switch
            {
                true => true,
                false => false,
                null => defaultReturn,
                _ => throw new ExpressionEvaluatorTypeErrorException($"Return was not boolean (value)"),
            };
        }
        catch (Exception e)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                $"Error while evaluating \"{property}\" on \"{context.Component?.PageId}.{context.Component?.Id}\"",
                e
            );
        }
    }

    /// <summary>
    /// Evaluate a <see cref="Expression" /> from a given <see cref="LayoutEvaluatorState" /> in a <see cref="ComponentContext" />
    /// </summary>
    public static async Task<object?> EvaluateExpression(
        LayoutEvaluatorState state,
        Expression expr,
        ComponentContext context,
        object?[]? positionalArguments = null
    )
    {
        if (!expr.IsFunctionExpression)
        {
            return expr.Value;
        }
        var args = new object?[expr.Args.Count];
        for (var i = 0; i < args.Length; i++)
        {
            // Some arguments should not be evaluated as expressions in order to keep compatibility with frontend.
            // This may be possible for this code to evaluate, but frontend may not be able to.
            if (expr.Function == ExpressionFunction.dataModel && i == 1 && expr.Args[i].IsFunctionExpression)
            {
                throw new ExpressionEvaluatorTypeErrorException(
                    "The data type must be a string (expressions cannot be used here)"
                );
            }
            if (expr.Function == ExpressionFunction.@if && i == 2 && expr.Args[i].IsFunctionExpression)
            {
                throw new ExpressionEvaluatorTypeErrorException("Expected third argument to be \"else\"");
            }
            if (expr.Function == ExpressionFunction.compare)
            {
                if (args.Length == 4 && i == 1 && expr.Args[i].IsFunctionExpression)
                {
                    throw new ExpressionEvaluatorTypeErrorException(
                        "Second argument must be \"not\" when providing 4 arguments in total"
                    );
                }
                if (args.Length == 4 && i == 2 && expr.Args[i].IsFunctionExpression)
                {
                    throw new ExpressionEvaluatorTypeErrorException(
                        "Invalid operator (it cannot be an expression or null)"
                    );
                }
                if (args.Length == 3 && i == 1 && expr.Args[i].IsFunctionExpression)
                {
                    throw new ExpressionEvaluatorTypeErrorException(
                        "Invalid operator (it cannot be an expression or null)"
                    );
                }
            }

            args[i] = await EvaluateExpression(state, expr.Args[i], context, positionalArguments);
        }
        // ! TODO: should find better ways to deal with nulls here for the next major version
        var ret = expr.Function switch
        {
            ExpressionFunction.dataModel => await DataModel(args, context, state),
            ExpressionFunction.component => await Component(args, context, state),
            ExpressionFunction.countDataElements => CountDataElements(args, state),
            ExpressionFunction.instanceContext => state.GetInstanceContext(args.First()?.ToString()!),
            ExpressionFunction.@if => IfImpl(args),
            ExpressionFunction.frontendSettings => state.GetFrontendSetting(args.First()?.ToString()!),
            ExpressionFunction.formatDate => FormatDate(args, state),
            ExpressionFunction.concat => Concat(args),
            ExpressionFunction.equals => EqualsImplementation(args),
            ExpressionFunction.notEquals => !EqualsImplementation(args),
            ExpressionFunction.greaterThanEq => GreaterThanEq(args),
            ExpressionFunction.lessThan => LessThan(args),
            ExpressionFunction.lessThanEq => LessThanEq(args),
            ExpressionFunction.greaterThan => GreaterThan(args),
            ExpressionFunction.and => And(args),
            ExpressionFunction.or => Or(args),
            ExpressionFunction.not => Not(args),
            ExpressionFunction.compare => Compare(args),
            ExpressionFunction.contains => Contains(args),
            ExpressionFunction.notContains => !Contains(args),
            ExpressionFunction.commaContains => CommaContains(args),
            ExpressionFunction.endsWith => EndsWith(args),
            ExpressionFunction.startsWith => StartsWith(args),
            ExpressionFunction.stringLength => StringLength(args),
            ExpressionFunction.stringIndexOf => StringIndexOf(args),
            ExpressionFunction.stringReplace => StringReplace(args),
            ExpressionFunction.stringSlice => StringSlice(args),
            ExpressionFunction.round => Round(args),
            ExpressionFunction.upperCase => UpperCase(args),
            ExpressionFunction.lowerCase => LowerCase(args),
            ExpressionFunction.lowerCaseFirst => LowerCaseFirst(args),
            ExpressionFunction.upperCaseFirst => UpperCaseFirst(args),
            ExpressionFunction.argv => Argv(args, positionalArguments),
            ExpressionFunction.gatewayAction => state.GetGatewayAction(),
            ExpressionFunction.language => state.GetLanguage() ?? "nb",
            _ => throw new ExpressionEvaluatorTypeErrorException($"Function \"{expr.Function}\" not implemented"),
        };
        return ret;
    }

    private static async Task<object?> DataModel(object?[] args, ComponentContext context, LayoutEvaluatorState state)
    {
        if (args is [DataReference dataReference])
        {
            return await DataModel(
                new ModelBinding() { Field = dataReference.Field },
                dataReference.DataElementIdentifier,
                context.RowIndices,
                state
            );
        }
        var key = args switch
        {
            [string field] => new ModelBinding { Field = field },
            [string field, string dataType] => new ModelBinding { Field = field, DataType = dataType },
            [ModelBinding binding] => binding,
            [null] => throw new ExpressionEvaluatorTypeErrorException("Cannot lookup dataModel null"),
            _ => throw new ExpressionEvaluatorTypeErrorException(
                $"""Expected ["dataModel", ...] to have 1-2 argument(s), got {args.Length}"""
            ),
        };
        return await DataModel(key, context.DataElementIdentifier, context.RowIndices, state);
    }

    private static async Task<object?> DataModel(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? indexes,
        LayoutEvaluatorState state
    )
    {
        var data = await state.GetModelData(key, defaultDataElementIdentifier, indexes);

        // Only allow IConvertible types to be returned from data model
        // Objects and arrays should return null
        return data switch
        {
            IConvertible c => c,
            _ => null,
        };
    }

    private static async Task<object?> Component(object?[] args, ComponentContext? context, LayoutEvaluatorState state)
    {
        var componentId = args.First()?.ToString();
        if (componentId is null)
        {
            throw new ArgumentException("Cannot lookup component null");
        }

        if (context?.Component is null)
        {
            throw new ArgumentException("The component expression requires a component context");
        }

        var targetContext = await state.GetComponentContext(context.Component.PageId, componentId, context.RowIndices);

        if (targetContext is null)
        {
            return null;
        }

        if (targetContext.Component is GroupComponent)
        {
            throw new NotImplementedException("Component lookup for components in groups not implemented");
        }

        if (targetContext.Component?.DataModelBindings.TryGetValue("simpleBinding", out var binding) != true)
        {
            throw new ArgumentException("component lookup requires the target component to have a simpleBinding");
        }
        if (await targetContext.IsHidden(state))
        {
            return null;
        }

        return await DataModel(binding, context.DataElementIdentifier, context.RowIndices, state);
    }

    private static int CountDataElements(object?[] args, LayoutEvaluatorState state)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument(s), got {args.Length}");
        }
        if (args[0] is not string dataType)
        {
            throw new ExpressionEvaluatorTypeErrorException("Expected dataType argument to be a string");
        }

        return state.CountDataElements(dataType);
    }

    private static string? Concat(object?[] args)
    {
        return string.Join(
            "",
            args.Select(a =>
                a switch
                {
                    string s => s,
                    _ => ToStringForEquals(a),
                }
            )
        );
    }

    private static bool Contains(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 argument(s), got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.Contains(stringTwo, StringComparison.InvariantCulture);
    }

    private static string? FormatDate(object?[] args, LayoutEvaluatorState? state)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1-2 argument(s), got {args.Length}");
        }

        DateTime? date = PrepareDateArg(args[0]);
        if (date is null)
        {
            return null;
        }

        if (date.GetValueOrDefault().Kind != DateTimeKind.Unspecified)
        {
            var targetTimeZone = state?.GetTimeZone() ?? TimeZoneInfo.Local;
            date = TimeZoneInfo.ConvertTime(date.GetValueOrDefault(), targetTimeZone);
        }

        string language = state?.GetLanguage() ?? "nb";
        return UnicodeDateTimeTokenConverter.Format(
            date,
            args.Length == 2 ? ToStringForEquals(args[1]) : null,
            language
        );
    }

    private static bool Compare(object?[] args)
    {
        if (args.Length < 3 || args.Length > 4)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 3-4 argument(s), got {args.Length}");
        }
        if (args.Length == 4 && args[1] is not null && args[1] as string != "not")
        {
            throw new ExpressionEvaluatorTypeErrorException(
                $"Second argument must be \"not\" when providing 4 arguments in total"
            );
        }
        var op = args.Length == 4 ? args[2] : args[1];
        var opString = op as string;
        if (opString is null)
        {
            throw new ExpressionEvaluatorTypeErrorException("Invalid operator (it cannot be an expression or null)");
        }

        var a = args[0];
        var b = args.Length == 4 ? args[3] : args[2];

        // For all operators except for 'equals', every call with any null argument should return false
        if (opString != "equals" && (a is null || b is null))
        {
            return false;
        }

        var passOnArgs = new[] { a, b };
        bool result = op switch
        {
            "equals" => EqualsImplementation(passOnArgs) ?? false,
            "greaterThan" => GreaterThan(passOnArgs) ?? false,
            "greaterThanEq" => GreaterThanEq(passOnArgs) ?? false,
            "lessThan" => LessThan(passOnArgs) ?? false,
            "lessThanEq" => LessThanEq(passOnArgs) ?? false,
            "isBefore" => DateIsBefore(passOnArgs),
            "isAfter" => DateIsAfter(passOnArgs),
            "isBeforeEq" => DateIsBeforeEq(passOnArgs),
            "isAfterEq" => DateIsAfterEq(passOnArgs),
            "isSameDay" => DateIsSameDay(passOnArgs),
            _ => throw new ExpressionEvaluatorTypeErrorException($"Invalid operator \"{opString}\""),
        };

        return args.Length == 4 ? !result : result;
    }

    private static DateTime? PrepareDateArg(object? arg)
    {
        var dateStr = ToStringForEquals(arg);
        if (arg is null || dateStr is null || dateStr == "")
        {
            return null;
        }

        var datePatterns = new[]
        {
            // These are duplicated in frontend, and limits the date formats that can be used in expressions to
            // only those that match these patterns (marking a baseline for what we support).
            @"^(\d{4})$",
            @"^(\d{4})-(\d{2})-(\d{2})T?$",
            @"^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})Z?([+-]\d{2}:\d{2})?$",
            @"^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2}):(\d{2})Z?([+-]\d{2}:\d{2})?$",
            @"^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2}):(\d{2})\.(\d+)Z?([+-]\d{2}:\d{2})?$",
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(
                dateStr,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ECMAScript
            );
            if (!match.Success)
            {
                continue;
            }
            if (match.Groups.Count == 2)
            {
                // If only a year is provided, assume January 1st
                return new DateTime(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), 1, 1);
            }

            try
            {
                return DateTime.Parse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
            catch (Exception)
            {
                throw new ExpressionEvaluatorTypeErrorException(
                    $"Unable to parse date \"{dateStr}\": Format was recognized, but the date/time is invalid"
                );
            }
        }

        if (dateStr.Trim() != "")
        {
            throw new ExpressionEvaluatorTypeErrorException($"Unable to parse date \"{dateStr}\": Unknown format");
        }

        return null;
    }

    private static (DateTime?, DateTime?) PrepareDateArgs(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException("Invalid number of args for date compare");
        }

        var a = PrepareDateArg(args[0]);
        var b = PrepareDateArg(args[1]);

        return (a, b);
    }

    private static bool DateIsBefore(object?[] args)
    {
        var (a, b) = PrepareDateArgs(args);
        return a < b;
    }

    private static bool DateIsAfter(object?[] args)
    {
        var (a, b) = PrepareDateArgs(args);
        return a > b;
    }

    private static bool DateIsBeforeEq(object?[] args)
    {
        var (a, b) = PrepareDateArgs(args);
        return a <= b;
    }

    private static bool DateIsAfterEq(object?[] args)
    {
        var (a, b) = PrepareDateArgs(args);
        return a >= b;
    }

    private static bool DateIsSameDay(object?[] args)
    {
        var (a, b) = PrepareDateArgs(args);
        return a?.Date == b?.Date;
    }

    private static bool EndsWith(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 argument(s), got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.EndsWith(stringTwo, StringComparison.InvariantCulture);
    }

    private static bool StartsWith(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 argument(s), got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.StartsWith(stringTwo, StringComparison.InvariantCulture);
    }

    private static bool CommaContains(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 arguments, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.Split(",").Select(s => s.Trim()).Contains(stringTwo, StringComparer.InvariantCulture);
    }

    private static int StringLength(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        return stringOne?.Length ?? 0;
    }

    private static int? StringIndexOf(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 arguments, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return null;
        }

        var idx = stringOne.IndexOf(stringTwo, StringComparison.InvariantCulture);
        return idx == -1 ? null : idx;
    }

    private static string? StringReplace(object?[] args)
    {
        if (args.Length != 3)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 3 arguments, got {args.Length}");
        }
        string? subject = ToStringForEquals(args[0]);
        string? search = ToStringForEquals(args[1]);
        string? replace = ToStringForEquals(args[2]);

        if (subject is null || search is null || replace is null || subject == "" || search == "")
        {
            return null;
        }

        return subject.Replace(search, replace, StringComparison.InvariantCulture);
    }

    private static string? StringSlice(object?[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2-3 arguments, got {args.Length}");
        }
        string? subject = ToStringForEquals(args[0]);
        double? start = PrepareNumericArg(args[1]);
        double? length = args.Length == 3 ? PrepareNumericArg(args[2]) : null;

        if (start == null)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                "Start index cannot be null (if you used an expression like stringIndexOf here, make sure to guard against null)"
            );
        }

        if (subject is null)
        {
            return null;
        }

        if (start < 0)
        {
            throw new ExpressionEvaluatorTypeErrorException("Start index cannot be negative");
        }

        if (length != null && length < 0)
        {
            throw new ExpressionEvaluatorTypeErrorException("Length cannot be negative");
        }

        if (length == null)
        {
            return subject.Substring((int)start);
        }

        return subject.Substring((int)start, (int)length);
    }

    private static string Round(object?[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1-2 argument(s), got {args.Length}");
        }

        var number = PrepareNumericArg(args[0]);

        if (number is null)
        {
            number = 0;
        }

        int precision = 0;
        if (args.Length == 2 && args[1] is not null)
        {
            precision = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
        }

        return number.Value.ToString($"N{precision}", CultureInfo.InvariantCulture);
    }

    private static string? UpperCase(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        return stringOne?.ToUpperInvariant();
    }

    private static string? LowerCase(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        return stringOne?.ToLowerInvariant();
    }

    private static string? UpperCaseFirst(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        if (stringOne is null)
        {
            return null;
        }
        return stringOne.Length switch
        {
            0 => stringOne,
            1 => stringOne.ToUpperInvariant(),
            _ => stringOne[0].ToString().ToUpperInvariant() + stringOne[1..],
        };
    }

    private static string? LowerCaseFirst(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        if (stringOne is null)
        {
            return null;
        }
        return stringOne.Length switch
        {
            0 => stringOne,
            1 => stringOne.ToLowerInvariant(),
            _ => stringOne[0].ToString().ToLowerInvariant() + stringOne[1..],
        };
    }

    private static bool PrepareBooleanArg(object? arg)
    {
        return arg switch
        {
            bool b => b,
            null => false,
            string s => s switch
            {
                "true" => true,
                "false" => false,
                "1" => true,
                "0" => false,
                _ => ParseNumber(s, throwException: false) switch
                {
                    1 => true,
                    0 => false,
                    _ => throw new ExpressionEvaluatorTypeErrorException($"Expected boolean, got value \"{s}\""),
                },
            },
            double s => s switch
            {
                1 => true,
                0 => false,
                _ => throw new ExpressionEvaluatorTypeErrorException($"Expected boolean, got value {s}"),
            },
            _ => throw new ExpressionEvaluatorTypeErrorException(
                "Unknown data type encountered in expression: " + arg.GetType().Name
            ),
        };
    }

    private static bool? And(object?[] args)
    {
        if (args.Length == 0)
        {
            throw new ExpressionEvaluatorTypeErrorException("Expected 1+ argument(s), got 0");
        }

        var preparedArgs = args.Select(arg => PrepareBooleanArg(arg)).ToArray();
        // Ensure all args gets converted, because they might throw an Exception
        return preparedArgs.All(a => a);
    }

    private static bool? Or(object?[] args)
    {
        if (args.Length == 0)
        {
            throw new ExpressionEvaluatorTypeErrorException("Expected 1+ argument(s), got 0");
        }

        var preparedArgs = args.Select(arg => PrepareBooleanArg(arg)).ToArray();
        // Ensure all args gets converted, because they might throw an Exception
        return preparedArgs.Any(a => a);
    }

    private static bool? Not(object?[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument(s), got {args.Length}");
        }
        return !PrepareBooleanArg(args[0]);
    }

    private static (double?, double?) PrepareNumericArgs(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException("Invalid number of args for compare");
        }

        var a = PrepareNumericArg(args[0]);

        var b = PrepareNumericArg(args[1]);

        return (a, b);
    }

    private static double? PrepareNumericArg(object? arg)
    {
        return arg switch
        {
            bool ab => throw new ExpressionEvaluatorTypeErrorException(
                $"Expected number, got value {(ab ? "true" : "false")}"
            ),
            string s => ParseNumber(s),
            IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    private static object? IfImpl(object?[] args)
    {
        if (args.Length == 2)
        {
            return PrepareBooleanArg(args[0]) ? args[1] : null;
        }

        if (args.Length > 2 && !"else".Equals(args[2] as string, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExpressionEvaluatorTypeErrorException("Expected third argument to be \"else\"");
        }

        if (args.Length == 4)
        {
            return PrepareBooleanArg(args[0]) ? args[1] : args[3];
        }

        throw new ExpressionEvaluatorTypeErrorException(
            "Expected either 2 arguments (if) or 4 (if + else), got " + args.Length
        );
    }

    private static readonly Regex _numberRegex = new Regex(@"^-?\d+(\.\d+)?$");

    private static double? ParseNumber(string s, bool throwException = true)
    {
        if (_numberRegex.IsMatch(s) && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        if (throwException)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected number, got value \"{s}\"");
        }
        return null;
    }

    private static bool? LessThan(object?[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handeling
        }
        return a < b; // Actual implementation
    }

    private static bool? LessThanEq(object?[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handeling
        }
        return a <= b; // Actual implementation
    }

    private static bool? GreaterThan(object?[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handeling
        }
        return a > b; // Actual implementation
    }

    private static bool? GreaterThanEq(object?[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handeling
        }
        return a >= b; // Actual implementation
    }

    internal static string? ToStringForEquals(object? value) =>
        value switch
        {
            null => null,
            bool bValue => bValue ? "true" : "false",
            // Special case for "TruE" to be equal to true
            string sValue when "true".Equals(sValue, StringComparison.OrdinalIgnoreCase) => "true",
            string sValue when "false".Equals(sValue, StringComparison.OrdinalIgnoreCase) => "false",
            string sValue when "null".Equals(sValue, StringComparison.OrdinalIgnoreCase) => null,
            string sValue => sValue,
            decimal decValue => decValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            uint uintValue => uintValue.ToString(CultureInfo.InvariantCulture),
            short shortValue => shortValue.ToString(CultureInfo.InvariantCulture),
            ushort ushortValue => ushortValue.ToString(CultureInfo.InvariantCulture),
            long longValue => longValue.ToString(CultureInfo.InvariantCulture),
            ulong ulongValue => ulongValue.ToString(CultureInfo.InvariantCulture),
            byte byteValue => byteValue.ToString(CultureInfo.InvariantCulture),
            sbyte sbyteValue => sbyteValue.ToString(CultureInfo.InvariantCulture),
            // BigInteger bigIntValue => bigIntValue.ToString(CultureInfo.InvariantCulture), // Big integer not supported in json
            DateTime dtValue => dtValue.ToString("o"),
            DateOnly dateValue => dateValue.ToString("o", CultureInfo.InvariantCulture),
            TimeOnly timeValue => timeValue.ToString("o", CultureInfo.InvariantCulture),
            //TODO: Consider having JsonSerializer as a fallback for everything (including arrays and objects)
            _ => throw new NotImplementedException(
                $"ToStringForEquals not implemented for type {value.GetType().Name}"
            ),
        };

    internal static bool? EqualsImplementation(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 argument(s), got {args.Length}");
        }

        return string.Equals(ToStringForEquals(args[0]), ToStringForEquals(args[1]), StringComparison.Ordinal);
    }

    private static object? Argv(object?[] args, object?[]? positionalArguments)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument(s), got {args.Length}");
        }

        var index = (int?)PrepareNumericArg(args[0]);
        if (!index.HasValue)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected number, got value \"{args[0]}\"");
        }

        if (positionalArguments == null)
        {
            throw new ExpressionEvaluatorTypeErrorException("No positional arguments available");
        }
        if (index < 0 || index >= positionalArguments.Length)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Index {index} out of range");
        }

        return positionalArguments[index.Value];
    }
}
