using System.Diagnostics;
using static Altinn.App.Core.Features.Telemetry.ApplicationMetadataClient;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetTextActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetText");
        return activity;
    }

    internal Activity? StartGetApplicationActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetApplication");
        return activity;
    }

    internal Activity? StartGetModelJsonSchemaActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetModelJsonSchema");
        return activity;
    }

    internal Activity? StartGetPrefillJsonActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetPrefillJson");
        return activity;
    }

    internal Activity? StartGetLayoutsActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayouts");
        return activity;
    }

    internal Activity? StartGetLayoutSetActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSet");
        return activity;
    }

    internal Activity? StartGetLayoutSetsActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSets");
        return activity;
    }

    internal Activity? StartGetLayoutsForSetActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutsForSet");
        return activity;
    }

    internal Activity? StartGetLayoutSetsForTaskActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSetsForTask");
        return activity;
    }

    internal Activity? StartGetLayoutSettingsActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSettings");
        return activity;
    }

    internal Activity? StartGetLayoutSettingsStringActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSettingsString");
        return activity;
    }

    internal Activity? StartGetLayoutSettingsForSetActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSettingsForSet");
        return activity;
    }

    internal Activity? StartGetLayoutSettingsStringForSetActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutSettingsStringForSet");
        return activity;
    }

    internal Activity? StartGetTextsActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetTexts");
        return activity;
    }

    internal Activity? StartGetRuleConfigurationForSetActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetRuleConfigurationForSet");
        return activity;
    }

    internal Activity? StartGetRuleHandlerForSetActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetRuleHandlerForSet");
        return activity;
    }

    internal Activity? StartGetFooterActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetFooter");
        return activity;
    }

    internal Activity? StartGetValidationConfigurationActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetValidationConfiguration");
        return activity;
    }

    internal Activity? StartGetLayoutModelActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetLayoutModel");
        return activity;
    }

    internal Activity? StartGetClassRefActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetClassRef");
        return activity;
    }

    internal Activity? StartClientGetApplicationXACMLPolicyActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetXACMLPolicy");
        return activity;
    }

    internal Activity? StartClientGetApplicationBPMNProcessActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetBPMNProcess");
        return activity;
    }

    internal static class ApplicationMetadataService
    {
        internal const string _prefix = "ApplicationMetadata.Service";
    }
}
