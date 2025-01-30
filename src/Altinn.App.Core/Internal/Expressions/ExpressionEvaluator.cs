using System.Globalization;
using System.Text.Json;
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

            var result = await EvaluateExpression_internal(state, expr, context);

            return result.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => defaultReturn,
                _ => throw new ExpressionEvaluatorTypeErrorException($"Return was not boolean. Was {result.Json}"),
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
        var positionalArgumentUnions = positionalArguments?.Select(ExpressionTypeUnion.FromObject).ToArray();
        var result = await EvaluateExpression_internal(state, expr, context, positionalArgumentUnions);
        return result.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => result.String,
            JsonValueKind.Number => result.Number,
            // JsonValueKind.Array => result.Array,
            // JsonValueKind.Object => result.Object,
            _ => throw new ExpressionEvaluatorTypeErrorException(
                $"Unexpected value kind {result.ValueKind} in expression evaluation"
            ),
        };
    }

    /// <summary>
    /// private implementation in order to change the types of positional arguments without breaking change.
    /// </summary>
    private static async Task<ExpressionTypeUnion> EvaluateExpression_internal(
        LayoutEvaluatorState state,
        Expression expr,
        ComponentContext context,
        ExpressionTypeUnion[]? positionalArguments = null
    )
    {
        if (!expr.IsFunctionExpression)
        {
            return expr.Value;
        }
        var args = new ExpressionTypeUnion[expr.Args.Count];
        for (var i = 0; i < args.Length; i++)
        {
            args[i] = await EvaluateExpression_internal(state, expr.Args[i], context, positionalArguments);
        }

        ExpressionTypeUnion ret = expr.Function switch
        {
            ExpressionFunction.dataModel => await DataModel(args, context, state),
            ExpressionFunction.component => await Component(args, context, state),
            ExpressionFunction.instanceContext => InstanceContext(state, args),
            ExpressionFunction.@if => IfImpl(args),
            ExpressionFunction.frontendSettings => FrontendSetting(state, args),
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
            ExpressionFunction.contains => Contains(args),
            ExpressionFunction.notContains => !Contains(args),
            ExpressionFunction.commaContains => CommaContains(args),
            ExpressionFunction.endsWith => EndsWith(args),
            ExpressionFunction.startsWith => StartsWith(args),
            ExpressionFunction.stringLength => StringLength(args),
            ExpressionFunction.round => Round(args),
            ExpressionFunction.upperCase => UpperCase(args),
            ExpressionFunction.lowerCase => LowerCase(args),
            ExpressionFunction.argv => Argv(args, positionalArguments),
            ExpressionFunction.gatewayAction => state.GetGatewayAction(),
            ExpressionFunction.language => state.GetLanguage() ?? "nb",
            _ => throw new ExpressionEvaluatorTypeErrorException("Function not implemented", expr.Function, args),
        };
        return ret;
    }

    private static string InstanceContext(LayoutEvaluatorState state, ExpressionTypeUnion[] args)
    {
        if (args is [{ ValueKind: JsonValueKind.String } arg])
        {
            return state.GetInstanceContext(arg.String);
        }
        throw new ExpressionEvaluatorTypeErrorException(
            "Unknown Instance context property type",
            ExpressionFunction.instanceContext,
            args
        );
    }

    private static string? FrontendSetting(LayoutEvaluatorState state, ExpressionTypeUnion[] args)
    {
        return args switch
        {
            [{ ValueKind: JsonValueKind.String } arg] => state.GetFrontendSetting(arg.String),
            [{ ValueKind: JsonValueKind.Null }] => throw new ExpressionEvaluatorTypeErrorException(
                "Value cannot be null. (Parameter 'key')",
                ExpressionFunction.frontendSettings,
                args
            ),
            _ => throw new ExpressionEvaluatorTypeErrorException(
                "Expected 1 argument",
                ExpressionFunction.frontendSettings,
                args
            ),
        };
    }

    private static async Task<ExpressionTypeUnion> DataModel(
        ExpressionTypeUnion[] args,
        ComponentContext context,
        LayoutEvaluatorState state
    )
    {
        ModelBinding key = args switch
        {
            [{ ValueKind: JsonValueKind.String } field] => new ModelBinding { Field = field.String },
            [{ ValueKind: JsonValueKind.String } field, { ValueKind: JsonValueKind.String } dataType] =>
                new ModelBinding { Field = field.String, DataType = dataType.String },
            [{ ValueKind: JsonValueKind.Null }] => throw new ExpressionEvaluatorTypeErrorException(
                "Cannot lookup dataModel null",
                ExpressionFunction.dataModel,
                args
            ),
            _ => throw new ExpressionEvaluatorTypeErrorException(
                "expected 1-2 argument(s)",
                ExpressionFunction.dataModel,
                args
            ),
        };
        return await DataModel(key, context.DataElementIdentifier, context.RowIndices, state);
    }

    private static async Task<ExpressionTypeUnion> DataModel(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? indexes,
        LayoutEvaluatorState state
    )
    {
        var data = await state.GetModelData(key, defaultDataElementIdentifier, indexes);

        return ExpressionTypeUnion.FromObject(data);
    }

    private static async Task<ExpressionTypeUnion> Component(
        ExpressionTypeUnion[] args,
        ComponentContext? context,
        LayoutEvaluatorState state
    )
    {
        var componentId = args switch
        {
            [{ ValueKind: JsonValueKind.String } arg] => arg.String,
            [{ } arg] => throw new ExpressionEvaluatorTypeErrorException(
                $"Cannot lookup component {arg.Json}",
                ExpressionFunction.component,
                args
            ),
            _ => throw new ExpressionEvaluatorTypeErrorException(
                $"Expected 1 argument",
                ExpressionFunction.component,
                args
            ),
        };

        if (context?.Component is null)
        {
            throw new ArgumentException("The component expression requires a component context");
        }

        var targetContext = await state.GetComponentContext(context.Component.PageId, componentId, context.RowIndices);

        if (targetContext is null)
        {
            return new ExpressionTypeUnion();
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
            return new ExpressionTypeUnion();
        }

        return await DataModel(binding, context.DataElementIdentifier, context.RowIndices, state);
    }

    private static string Concat(ExpressionTypeUnion[] args)
    {
        return string.Join(
            "",
            args.Select(a =>
                a.ValueKind switch
                {
                    JsonValueKind.String => a.String,
                    _ => ToStringForEquals(a),
                }
            )
        );
    }

    private static bool Contains(ExpressionTypeUnion[] args)
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

    private static bool EndsWith(ExpressionTypeUnion[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                "Expected 2 argument(s)",
                ExpressionFunction.endsWith,
                args
            );
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.EndsWith(stringTwo, StringComparison.InvariantCulture);
    }

    private static bool StartsWith(ExpressionTypeUnion[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                "Expected 2 argument(s)",
                ExpressionFunction.startsWith,
                args
            );
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.StartsWith(stringTwo, StringComparison.InvariantCulture);
    }

    private static bool CommaContains(ExpressionTypeUnion[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                "expect 2 arguments",
                ExpressionFunction.commaContains,
                args
            );
        }
        string? stringOne = ToStringForEquals(args[0]);
        string? stringTwo = ToStringForEquals(args[1]);

        if (stringOne is null || stringTwo is null)
        {
            return false;
        }

        return stringOne.Split(",").Select(s => s.Trim()).Contains(stringTwo, StringComparer.InvariantCulture);
    }

    private static int StringLength(ExpressionTypeUnion[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"1 argument", ExpressionFunction.stringLength, args);
        }
        string? stringOne = ToStringForEquals(args[0]);
        return stringOne?.Length ?? 0;
    }

    private static string Round(ExpressionTypeUnion[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                $"Expected 1-2 argument(s), got {args.Length}",
                ExpressionFunction.round,
                args
            );
        }

        var number = PrepareNumericArg(args[0]);

        if (number is null)
        {
            number = 0;
        }

        int precision = 0;

        if (args.Length == 2)
        {
            precision = (int)(PrepareNumericArg(args[1]) ?? 0);
        }

        return number.Value.ToString($"N{precision}", CultureInfo.InvariantCulture);
    }

    private static string? UpperCase(ExpressionTypeUnion[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        return stringOne?.ToUpperInvariant();
    }

    private static string? LowerCase(ExpressionTypeUnion[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument, got {args.Length}");
        }
        string? stringOne = ToStringForEquals(args[0]);
        return stringOne?.ToLowerInvariant();
    }

    private static bool PrepareBooleanArg(ExpressionTypeUnion arg)
    {
        return arg.ValueKind switch
        {
            JsonValueKind.Null => false,
            JsonValueKind.True => true,
            JsonValueKind.False => false,

            JsonValueKind.String => arg.String switch
            {
                "true" => true,
                "false" => false,
                "1" => true,
                "0" => false,
                _ => ParseNumber(arg.String, throwException: false) switch
                {
                    1 => true,
                    0 => false,
                    _ => throw new ExpressionEvaluatorTypeErrorException(
                        $"Expected boolean, got value \"{arg.String}\""
                    ),
                },
            },
            JsonValueKind.Number => arg.Number switch
            {
                1 => true,
                0 => false,
                _ => throw new ExpressionEvaluatorTypeErrorException($"Expected boolean, got value {arg.Number}"),
            },
            _ => throw new ExpressionEvaluatorTypeErrorException(
                "Unknown data type encountered in expression: " + arg.ValueKind
            ),
        };
    }

    private static bool? And(ExpressionTypeUnion[] args)
    {
        if (args.Length == 0)
        {
            throw new ExpressionEvaluatorTypeErrorException("Expected 1+ argument(s), got 0");
        }

        var preparedArgs = args.Select(arg => PrepareBooleanArg(arg)).ToArray();
        // Ensure all args gets converted, because they might throw an Exception
        return preparedArgs.All(a => a);
    }

    private static bool? Or(ExpressionTypeUnion[] args)
    {
        if (args.Length == 0)
        {
            throw new ExpressionEvaluatorTypeErrorException("Expected 1+ argument(s), got 0");
        }

        var preparedArgs = args.Select(arg => PrepareBooleanArg(arg)).ToArray();
        // Ensure all args gets converted, because they might throw an Exception
        return preparedArgs.Any(a => a);
    }

    private static bool? Not(ExpressionTypeUnion[] args)
    {
        if (args.Length != 1)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 1 argument(s), got {args.Length}");
        }
        return !PrepareBooleanArg(args[0]);
    }

    private static (double?, double?) PrepareNumericArgs(ExpressionTypeUnion[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException("Invalid number of args for compare");
        }

        var a = PrepareNumericArg(args[0]);

        var b = PrepareNumericArg(args[1]);

        return (a, b);
    }

    private static double? PrepareNumericArg(ExpressionTypeUnion arg)
    {
        return arg.ValueKind switch
        {
            JsonValueKind.True or JsonValueKind.False or JsonValueKind.Array or JsonValueKind.Object =>
                throw new ExpressionEvaluatorTypeErrorException($"Expected number, got value {arg.Json}"),
            JsonValueKind.String => ParseNumber(arg.String),
            JsonValueKind.Number => arg.Number,

            _ => null,
        };
    }

    private static ExpressionTypeUnion IfImpl(ExpressionTypeUnion[] args)
    {
        if (args.Length == 2)
        {
            return PrepareBooleanArg(args[0]) ? args[1] : new ExpressionTypeUnion();
        }

        if (
            args.Length > 2
            && !(
                args[2].ValueKind == JsonValueKind.String
                && "else".Equals(args[2].String, StringComparison.OrdinalIgnoreCase)
            )
        )
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

    private static bool LessThan(ExpressionTypeUnion[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handeling
        }
        return a < b; // Actual implementation
    }

    private static bool LessThanEq(ExpressionTypeUnion[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handeling
        }
        return a <= b; // Actual implementation
    }

    private static bool GreaterThan(ExpressionTypeUnion[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // error handling
        }
        return a > b; // Actual implementation
    }

    private static bool GreaterThanEq(ExpressionTypeUnion[] args)
    {
        var (a, b) = PrepareNumericArgs(args);

        if (a is null || b is null)
        {
            return false; // false if any is null
        }
        return a >= b; // Actual implementation
    }

    internal static string? ToStringForEquals(ExpressionTypeUnion value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => value.String switch
            {
                // Special case for "TruE" to be equal to true
                { } sValue when sValue.Equals("true", StringComparison.OrdinalIgnoreCase) => "true",
                { } sValue when sValue.Equals("false", StringComparison.OrdinalIgnoreCase) => "false",
                { } sValue when sValue.Equals("null", StringComparison.OrdinalIgnoreCase) => null,
                { } sValue => sValue,
            },
            JsonValueKind.Number => value.Number.ToString(CultureInfo.InvariantCulture),
            _ => throw new NotImplementedException($"ToStringForEquals not implemented for {value.ValueKind}"),
        };

    internal static bool EqualsImplementation(ExpressionTypeUnion[] args)
    {
        if (args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 argument(s), got {args.Length}");
        }

        return string.Equals(ToStringForEquals(args[0]), ToStringForEquals(args[1]), StringComparison.Ordinal);
    }

    private static ExpressionTypeUnion Argv(ExpressionTypeUnion[] args, ExpressionTypeUnion[]? positionalArguments)
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
