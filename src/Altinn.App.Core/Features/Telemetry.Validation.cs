using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Core.Features.Telemetry.Validation;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    private void InitValidation() { }

    internal Activity? StartValidateInstanceAtTaskActivity(Instance instance, string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        var activity = ActivitySource.StartActivity($"{_prefix}.ValidateInstanceAtTask");
        activity.SetTaskId(taskId);
        activity.SetInstanceId(instance);
        return activity;
    }

    internal Activity? StartRunTaskValidatorActivity(ITaskValidator validator)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.RunTaskValidator");

        if (activity is not null)
        {
            activity.SetTag(LabelValidatorType, validator.GetType().Name);
            activity.SetTag(LabelValidatorSource, validator.ValidationSource);
        }
        return activity;
    }

    internal Activity? StartValidateDataElementActivity(Instance instance, DataElement dataElement)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.ValidateDataElement");
        activity.SetInstanceId(instance);
        activity.SetDataElementId(dataElement);
        return activity;
    }

    internal Activity? StartRunDataElementValidatorActivity(IDataElementValidator validator)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.RunDataElementValidator");

        if (activity is not null)
        {
            activity.SetTag(LabelValidatorType, validator.GetType().Name);
            activity.SetTag(LabelValidatorSource, validator.ValidationSource);
        }
        return activity;
    }

    internal Activity? StartValidateFormDataActivity(Instance instance, DataElement dataElement)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.ValidateFormData");

        activity.SetInstanceId(instance);
        activity.SetDataElementId(dataElement);
        return activity;
    }

    internal Activity? StartRunFormDataValidatorActivity(IFormDataValidator validator)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.RunFormDataValidator");

        if (activity is not null)
        {
            activity.SetTag(LabelValidatorType, validator.GetType().Name);
            activity.SetTag(LabelValidatorSource, validator.ValidationSource);
        }

        return activity;
    }

    internal static class Validation
    {
        internal const string _prefix = "Validation";

        internal const string LabelValidatorType = "validator.type";
        internal const string LabelValidatorSource = "validator.source";
    }
}
