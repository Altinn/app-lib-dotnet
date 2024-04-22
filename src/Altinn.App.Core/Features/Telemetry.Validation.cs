using System.Diagnostics;
using static Altinn.App.Core.Features.Telemetry.Validation;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    private void InitValidation()
    {
    }

    internal Activity? StartValidateInstanceAtTaskActivity(Guid instanceGuid, string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        var activity = ActivitySource.StartActivity(ValidateInstanceAtTaskTraceName);
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.TaskId, taskId);
        return activity;
    }

    internal Activity? StartRunTaskValidatorActivity(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        var activity = ActivitySource.StartActivity(RunTaskValidatorTraceName);
        activity?.SetTag(Labels.TaskId, taskId);
        return activity;
    }

    internal Activity? StartValidateDataElementActivity(Guid instanceGuid, Guid dataGuid)
    {
        var activity = ActivitySource.StartActivity(ValidateDataElementTraceName);
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.DataGuid, dataGuid);
        return activity;
    }

    internal Activity? StartRunDataElementValidatorActivity(string dataType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        var activity = ActivitySource.StartActivity(RunDataElementValidatorTraceName);
        return activity;
    }

    internal Activity? StartValidateFormDataActivity()
    {
        var activity = ActivitySource.StartActivity(ValidateFormDataTraceName);
        return activity;
    }

    internal Activity? StartRunFormDataValidatorActivity()
    {
        var activity = ActivitySource.StartActivity(RunFormDataValidatorTraceName);
        return activity;
    }

    internal static class Validation
    {
        private static readonly string _prefix = "Validation";

        internal static readonly string RunTaskValidatorTraceName = $"{_prefix}.RunTaskValidator";
        internal static readonly string ValidateInstanceAtTaskTraceName = $"{_prefix}.ValidateInstanceAtTask";
        internal static readonly string ValidateDataElementTraceName = $"{_prefix}.ValidateDataElement";
        internal static readonly string RunDataElementValidatorTraceName = $"{_prefix}.RunDataElementValidator";
        internal static readonly string ValidateFormDataTraceName = $"{_prefix}.ValidateFormData";
        internal static readonly string RunFormDataValidatorTraceName = $"{_prefix}.RunFormDataValidator";
    }
}