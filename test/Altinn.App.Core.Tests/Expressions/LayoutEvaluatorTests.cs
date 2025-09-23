// Testing library and framework: xUnit with FluentAssertions (preferred if available); fallback to Assert.Equal if FluentAssertions not present.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Tests.Expressions;

public class LayoutEvaluatorTests
{
}
 

#region Test Doubles

file class FakeComponent : BaseComponent
{
    public FakeComponent(string id, string pageId, string layoutId, Dictionary<string, string> dataModelBindings, bool isRepeatingGroup = false, bool isRepeatingGroupRow = false)
        : base(id, pageId, layoutId, dataModelBindings)
    {
        IsRepeatingGroup = isRepeatingGroup;
        IsRepeatingGroupRow = isRepeatingGroupRow;
    }

    public bool IsRepeatingGroup { get; }
    public bool IsRepeatingGroupRow { get; }
}

// Minimal base to avoid referencing concrete framework components in tests.
// This mirrors only the members used by LayoutEvaluator.
public abstract class BaseComponent
{
    public string Id { get; }
    public string PageId { get; }
    public string LayoutId { get; }
    public Dictionary<string, string> DataModelBindings { get; }

    protected BaseComponent(string id, string pageId, string layoutId, Dictionary<string, string> dataModelBindings)
    {
        Id = id;
        PageId = pageId;
        LayoutId = layoutId;
        DataModelBindings = dataModelBindings ?? new Dictionary<string, string>();
    }
}

file class FakeComponentContext : ComponentContext
{
    public FakeComponentContext(
        BaseComponent component,
        IEnumerable<ComponentContext>? children = null,
        bool hidden = false,
        string? dataElementIdentifier = null,
        IReadOnlyDictionary<string, int>? rowIndices = null)
    {
        Component = component as dynamic; // dynamic to satisfy the consumer while keeping this fake local
        _hidden = hidden;
        ChildContexts = children?.ToList() ?? new List<ComponentContext>();
        DataElementIdentifier = dataElementIdentifier ?? "0";
        RowIndices = rowIndices ?? new Dictionary<string, int>();
    }

    private readonly bool _hidden;

    public override Task<bool> IsHidden(LayoutEvaluatorState state) => Task.FromResult(_hidden);
}

file class FakeDataReference : DataReference
{
    public FakeDataReference(string field, string dataElementIdentifier = "0")
    {
        Field = field;
        DataElementIdentifier = dataElementIdentifier;
    }

    public override string Field { get; }
    public override object DataElementIdentifier { get; }
}

file class FakeLayoutEvaluatorState : LayoutEvaluatorState
{
    private readonly List<ComponentContext> _contexts;
    private readonly Dictionary<string, object?> _model = new();
    private readonly Func<ComponentContext, string, bool>? _requiredRule;

    public readonly List<(DataReference Ref, RowRemovalOption Opt)> Removed = new();

    public FakeLayoutEvaluatorState(IEnumerable<ComponentContext> contexts, Func<ComponentContext, string, bool>? requiredRule = null)
    {
        _contexts = contexts.ToList();
        _requiredRule = requiredRule;
    }

    public void SetModelValue(string key, object? value) => _model[key] = value;

    public override Task<List<ComponentContext>> GetComponentContexts() => Task.FromResult(_contexts);

    public override Task<DataReference> AddInidicies(string binding, ComponentContext context)
    {
        // Simulate index expansion by replacing any {index} tokens based on RowIndices
        string expanded = binding;
        if (context.RowIndices is not null)
        {
            foreach (var kv in context.RowIndices)
            {
                expanded = expanded.Replace("{" + kv.Key + "}", kv.Value.ToString());
            }
        }
        return Task.FromResult<DataReference>(new FakeDataReference(expanded, context.DataElementIdentifier));
    }

    public override Task RemoveDataField(DataReference dataReference, RowRemovalOption rowRemovalOption)
    {
        Removed.Add((dataReference, rowRemovalOption));
        _model.Remove(dataReference.Field);
        return Task.CompletedTask;
    }

    public override Task<object?> GetModelData(string binding, object dataElementIdentifier, IReadOnlyDictionary<string, int>? rowIndices)
    {
        // Expand indices similarly
        string key = binding;
        if (rowIndices is not null)
        {
            foreach (var kv in rowIndices)
            {
                key = key.Replace("{" + kv.Key + "}", kv.Value.ToString());
            }
        }

        _model.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    // Helper method used by the test to evaluate required rule deterministically
    public bool IsRequired(ComponentContext ctx, string property) => _requiredRule?.Invoke(ctx, property) ?? false;
}

#endregion

#region Tests for GetHiddenFieldsForRemoval

public class GetHiddenFieldsForRemovalTests
{
    [Fact]
    public async Task Returns_empty_when_no_components()
    {
        var state = new FakeLayoutEvaluatorState(Array.Empty<ComponentContext>());

        var result = await LayoutEvaluator.GetHiddenFieldsForRemoval(state);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Includes_bindings_from_hidden_components_only()
    {
        var visibleComponent = new FakeComponent("c1", "p1", "l1", new Dictionary<string, string> { ["simple"] = "model.visible" });
        var hiddenComponent = new FakeComponent("c2", "p1", "l1", new Dictionary<string, string> { ["simple"] = "model.hidden" });

        var visibleCtx = new FakeComponentContext(visibleComponent, hidden: false);
        var hiddenCtx = new FakeComponentContext(hiddenComponent, hidden: true);

        var state = new FakeLayoutEvaluatorState(new[] { visibleCtx, hiddenCtx });

        var result = await LayoutEvaluator.GetHiddenFieldsForRemoval(state);

        // Only hidden binding should be returned
        Assert.Single(result);
        Assert.Equal("model.hidden", result[0].Field);
    }

    [Fact]
    public async Task Hidden_group_binding_is_included_when_group_is_hidden_and_not_when_visible()
    {
        var hiddenGroup = new FakeComponent("g1", "p1", "l1",
            new Dictionary<string, string> { ["group"] = "groupBinding", ["simple"] = "groupBinding[{row}].field" },
            isRepeatingGroup: true);

        var visibleGroup = new FakeComponent("g2", "p1", "l1",
            new Dictionary<string, string> { ["group"] = "groupBinding2", ["simple"] = "groupBinding2[{row}].field" },
            isRepeatingGroup: true);

        var hiddenGroupCtx = new FakeComponentContext(hiddenGroup, hidden: true, rowIndices: new Dictionary<string, int> { ["row"] = 0 });
        var visibleGroupCtx = new FakeComponentContext(visibleGroup, hidden: false, rowIndices: new Dictionary<string, int> { ["row"] = 1 });

        var state = new FakeLayoutEvaluatorState(new[] { hiddenGroupCtx, visibleGroupCtx });

        var result = await LayoutEvaluator.GetHiddenFieldsForRemoval(state);
        var fields = result.Select(r => r.Field).ToHashSet();

        Assert.Contains("groupBinding", fields);
        Assert.DoesNotContain("groupBinding2", fields);
    }

    [Fact]
    public async Task Does_not_remove_group_binding_when_group_is_visible_even_if_children_hidden()
    {
        var childHidden = new FakeComponent("c-child", "p1", "l1", new Dictionary<string, string> { ["simple"] = "groupX[{row}].child" });
        var visibleGroup = new FakeComponent("g", "p1", "l1", new Dictionary<string, string> { ["group"] = "groupX" }, isRepeatingGroup: true);

        var childCtx = new FakeComponentContext(childHidden, hidden: true, rowIndices: new Dictionary<string, int> { ["row"] = 0 });
        var groupCtx = new FakeComponentContext(visibleGroup, new[] { childCtx }, hidden: false, rowIndices: new Dictionary<string, int> { ["row"] = 0 });

        var state = new FakeLayoutEvaluatorState(new[] { groupCtx });

        var result = await LayoutEvaluator.GetHiddenFieldsForRemoval(state);
        var fields = result.Select(r => r.Field).ToHashSet();

        // groupX should not be included for removal because the group is visible
        Assert.DoesNotContain("groupX", fields);
        // child binding should be considered for removal as it's hidden within visible group
        Assert.Contains("groupX[0].child", fields);
    }

    [Fact]
    public async Task Throws_when_context_component_is_null()
    {
        // We simulate a null component by creating a derived context that returns null for Component
        var badCtx = new NullComponentContext(hidden: true);
        var state = new FakeLayoutEvaluatorState(new[] { badCtx });

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await LayoutEvaluator.GetHiddenFieldsForRemoval(state);
        });
    }

    private sealed class NullComponentContext : ComponentContext
    {
        private readonly bool _hidden;
        public NullComponentContext(bool hidden) { _hidden = hidden; }
        public override Task<bool> IsHidden(LayoutEvaluatorState state) => Task.FromResult(_hidden);
        // Component remains null
    }
}

#endregion

#region Tests for RemoveHiddenDataAsync

public class RemoveHiddenDataAsyncTests
{
    [Fact]
    public async Task Removes_only_fields_returned_by_GetHiddenFieldsForRemoval()
    {
        // Build two components: one hidden, one visible
        var hiddenComponent = new FakeComponent("c2", "p1", "l1", new Dictionary<string, string> { ["simple"] = "data.hidden" });
        var visibleComponent = new FakeComponent("c1", "p1", "l1", new Dictionary<string, string> { ["simple"] = "data.visible" });

        var hiddenCtx = new FakeComponentContext(hiddenComponent, hidden: true);
        var visibleCtx = new FakeComponentContext(visibleComponent, hidden: false);

        var state = new FakeLayoutEvaluatorState(new[] { hiddenCtx, visibleCtx });
        // Pre-populate model
        state.SetModelValue("data.hidden", "X");
        state.SetModelValue("data.visible", "Y");

        await LayoutEvaluator.RemoveHiddenDataAsync(state, RowRemovalOption.KeepEmptyRows);

        // Ensure only hidden was removed
        Assert.Contains(state.Removed, x => x.Ref.Field == "data.hidden");
        Assert.DoesNotContain(state.Removed, x => x.Ref.Field == "data.visible");
    }
}

#endregion

#region Tests for RunLayoutValidationsForRequired

public class RunLayoutValidationsForRequiredTests
{
    [Fact]
    public async Task Produces_validation_issue_when_required_and_value_is_null()
    {
        var comp = new FakeComponent("c1", "p1", "l1", new Dictionary<string, string> { ["simple"] = "m.requiredField" });
        var ctx = new FakeComponentContext(comp, hidden: false);
        var state = new FakeLayoutEvaluatorState(new[] { ctx },
            requiredRule: (c, property) => property == "required" && (c as FakeComponentContext) \!= null);

        // No value set => null
        var issues = await LayoutEvaluator.RunLayoutValidationsForRequired(state);

        var issue = Assert.Single(issues);
        Assert.Equal("required", issue.Code);
        Assert.Equal("backend.validation_errors.required", issue.CustomTextKey);
        Assert.Equal("m.requiredField", issue.Field);
    }

    [Fact]
    public async Task No_issue_when_not_required()
    {
        var comp = new FakeComponent("c1", "p1", "l1", new Dictionary<string, string> { ["simple"] = "m.optionalField" });
        var ctx = new FakeComponentContext(comp, hidden: false);
        var state = new FakeLayoutEvaluatorState(new[] { ctx },
            requiredRule: (c, property) => false);

        var issues = await LayoutEvaluator.RunLayoutValidationsForRequired(state);

        Assert.Empty(issues);
    }

    [Fact]
    public async Task Skips_hidden_components()
    {
        var comp = new FakeComponent("c1", "p1", "l1", new Dictionary<string, string> { ["simple"] = "m.hiddenRequired" });
        var ctx = new FakeComponentContext(comp, hidden: true);
        var state = new FakeLayoutEvaluatorState(new[] { ctx },
            requiredRule: (c, property) => true);

        var issues = await LayoutEvaluator.RunLayoutValidationsForRequired(state);

        Assert.Empty(issues);
    }

    [Fact]
    public async Task Handles_multiple_bindings_and_children()
    {
        var parent = new FakeComponent("parent", "p1", "l1", new Dictionary<string, string> { ["simple"] = "m.p", ["extra"] = "m.q" });
        var child = new FakeComponent("child", "p1", "l1", new Dictionary<string, string> { ["simple"] = "m.c" });

        var childCtx = new FakeComponentContext(child, hidden: false);
        var parentCtx = new FakeComponentContext(parent, new[] { childCtx }, hidden: false);

        var state = new FakeLayoutEvaluatorState(new[] { parentCtx },
            requiredRule: (c, property) => true);

        // Provide value only for m.q, leaving m.p and m.c null
        state.SetModelValue("m.q", "value");

        var issues = await LayoutEvaluator.RunLayoutValidationsForRequired(state);
        // Two missing: m.p and m.c
        Assert.Equal(2, issues.Count);
        Assert.Contains(issues, i => i.Field == "m.p");
        Assert.Contains(issues, i => i.Field == "m.c");
    }
}

#endregion

// Minimal abstractions to match method signatures used by LayoutEvaluator without depending on full framework classes.
// In real codebase these are coming from Altinn.App.Core, but for isolated unit tests we keep lean contracts.
public abstract class ComponentContext
{
    public virtual BaseComponent? Component { get; protected set; }
    public virtual List<ComponentContext> ChildContexts { get; protected set; } = new();
    public virtual string DataElementIdentifier { get; protected set; } = "0";
    public virtual IReadOnlyDictionary<string, int>? RowIndices { get; protected set; }

    public abstract Task<bool> IsHidden(LayoutEvaluatorState state);
}

public abstract class LayoutEvaluatorState
{
    public abstract Task<List<ComponentContext>> GetComponentContexts();
    public abstract Task<DataReference> AddInidicies(string binding, ComponentContext context);
    public abstract Task RemoveDataField(DataReference dataReference, RowRemovalOption rowRemovalOption);
    public abstract Task<object?> GetModelData(string binding, object dataElementIdentifier, IReadOnlyDictionary<string, int>? rowIndices);
}

public abstract class DataReference
{
    public abstract string Field { get; }
    public abstract object DataElementIdentifier { get; }
}

public enum RowRemovalOption
{
    KeepEmptyRows,
    RemoveRowIfAllFieldsEmpty
}

#endregion
