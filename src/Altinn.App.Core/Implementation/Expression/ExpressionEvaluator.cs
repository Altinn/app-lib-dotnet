using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Altinn.App.Core.Implementation.Expression;

public static class ExpressionEvaluator
{


    public static bool EvaluateBooleanExpression(LayoutEvaluatorState state, Component component, string property, bool defaultReturn, ComponentContext? context)
    {
        if (!component.Element.TryGetProperty(property, out var jsonExpression))
        {
            return defaultReturn;
        }

        var expr = jsonExpression.Deserialize<LayoutExpression>();
        if (expr is null)
        {
            return defaultReturn;
        }

        return EvaluateExpression(state, expr, context) switch
        {
            true => true,
            false => false,
            null => defaultReturn,
            _ => throw new Exception($"Return from evaluating \"{property}\" on \"{(component.Element.TryGetProperty("id", out var idElement) ? idElement.GetString() : "Unknown component")}\" was not boolean (value)")
        };
    }

    public static object? EvaluateExpression(LayoutEvaluatorState state, LayoutExpression expr, ComponentContext? context)
    {
        if(expr is null)
        {
            return null;
        }
        if (expr.Function is null || expr.Args is null)
        {
            return expr.Value;
        }

        var args = expr.Args.Select(a => EvaluateExpression(state, a, context)).ToArray();
        var ret = expr.Function switch
        {
            "dataModel" => state.GetModelData(args.First()?.ToString()!, context),
            "component" => state.GetComponentData(args.First()?.ToString()!, context),
            "instanceContext" => state.GetInstanceContext(args.First()?.ToString()!),
            "applicationSettings" => state.GetApplicationSetting(args.First()?.ToString()!),
            "concat" => Concat(args),
            "equals" => EqualsImplementation(args),
            "notEquals" => !EqualsImplementation(args),
            "greaterThanEq" => GreaterThanEq(args),
            "lessThan" => LessThan(args),
            "lessThanEq" => LessThanEq(args),
            "greaterThan" => GreaterThan(args),
            "and" => And(args),
            "or" => Or(args),
            // "<" => args.First() <  args.ElementAt(1),
            // ">" => args.First() > args.ElementAt(1),
            _ => throw new Exception($"Function \"{expr.Function}\" not implemented"),
        };
        return ret;
    }


    private static string? Concat(object?[] args)
    {
        return string.Join("", args.Select(a=> a switch{string s => s, _ => ToStringForEquals(a)}));
    }

    private static bool PrepareBooleanArg(object? arg)
    {
        return arg switch
        {
            bool b => b == true,
            null => false,
            string s => s switch
            {
                "true" => true,
                "false" => false,
                "1" => true,
                "0" => false,
                _ => parseNumber(s, throwException: false) switch
                {
                    1 => true,
                    0 => false,
                    _ => throw new Exception($"Expected boolean, got value \"{s}\""),
                }
            },
            double s => s switch
            {
                1 => true,
                0 => false,
                _ => throw new Exception($"Expected boolean, got value {s}"),
            },
            _ => throw new Exception("TODO: FIxme"),
        };
    }

    private static bool? And(object?[] args)
    {
        if (args.Length == 0)
        {
            throw new Exception("Expected 1+ argument(s), got 0");
        }

        var preparedArgs = args.Select(arg => PrepareBooleanArg(arg)).ToArray();
        // Ensure all args gets converted, because they might throw an Exception
        return preparedArgs.All(a => a);
    }

    private static bool? Or(object?[] args)
    {
        if (args.Length == 0)
        {
            throw new Exception("Expected 1+ argument(s), got 0");
        }

        var preparedArgs = args.Select(arg => PrepareBooleanArg(arg)).ToArray();
        // Ensure all args gets converted, because they might throw an Exception
        return preparedArgs.Any(a => a);
    }

    private static (double?, double?) PrepareNumericArgs(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new Exception("Invalid number of args for compare");
        }
        var a = args[0] switch
        {
            bool ab => throw new Exception($"Expected number, got value {(ab ? "true" : "false")}"),
            string s => parseNumber(s),
            object o => o as double?, // assume all relevant numers are representable as double (as in frontend)
            _ => null,
        };

        var b = args[1] switch
        {
            bool bb => throw new Exception($"Expected number, got value {(bb ? "true" : "false")}"),
            string s => parseNumber(s),
            object o => o as double?, // assume all relevant numers are representable as double (as in frontend)
            _ => null,
        };

        return (a, b);
    }

    private static readonly Regex numberRegex = new Regex(@"^-?\d+(\.\d+)?$");
    private static double? parseNumber(string s, bool throwException = true)
    {
        if (numberRegex.IsMatch(s) && double.TryParse(s, out var d))
        {
            return d;
        }

        if (throwException)
        {
            throw new Exception($"Expected number, got value \"{s}\"");
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

    private static string? ToStringForEquals(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is bool bvalue)
        {
            return bvalue ? "true" : "false";
        }

        if (value is string svalue)
        {
            if ("true".Equals(svalue, StringComparison.InvariantCultureIgnoreCase))
            {
                return "true";
            }
            else if ("false".Equals(svalue, StringComparison.InvariantCultureIgnoreCase))
            {
                return "false";
            }
            else if ("null".Equals(svalue, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return svalue;
        }
        else if (value is decimal decvalue)
        {
            return decvalue.ToString(CultureInfo.InvariantCulture);
        }
        else if (value is double doubvalue)
        {
            return doubvalue.ToString(CultureInfo.InvariantCulture);
        }

        //TODO: consider accepting more types that might be used in model (eg Datetime)
        throw new NotImplementedException();
    }

    private static bool? EqualsImplementation(object?[] args)
    {
        if (args.Length != 2)
        {
            throw new Exception($"Expected 2 argument(s), got {args.Length}");
        }

        return string.Equals(ToStringForEquals(args[0]), ToStringForEquals(args[1]), StringComparison.InvariantCulture);
    }
}

