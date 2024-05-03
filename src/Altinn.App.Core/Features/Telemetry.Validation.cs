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

        var activity = ActivitySource.StartActivity(TraceNameValidateInstanceAtTask);
        activity.SetTaskId(taskId);
        activity.SetInstanceId(instance);
        return activity;
    }

    internal Activity? StartRunTaskValidatorActivity(ITaskValidator validator)
    {
        var activity = ActivitySource.StartActivity(TraceNameRunTaskValidator);

        if (activity is not null)
        {
            activity.SetTag(LabelValidatorType, validator.GetType().Name);
            activity.SetTag(LabelValidatorSource, validator.ValidationSource);
        }
        return activity;
    }

    internal Activity? StartValidateDataElementActivity(Instance instance, DataElement dataElement)
    {
        var activity = ActivitySource.StartActivity(TraceNameValidateDataElement);
        activity.SetInstanceId(instance);
        activity.SetDataElementId(dataElement);
        return activity;
    }

    internal Activity? StartRunDataElementValidatorActivity(IDataElementValidator validator)
    {
        var activity = ActivitySource.StartActivity(TraceNameRunDataElementValidator);

        if (activity is not null)
        {
            activity.SetTag(LabelValidatorType, validator.GetType().Name);
            activity.SetTag(LabelValidatorSource, validator.ValidationSource);
        }
        return activity;
    }

    internal Activity? StartValidateFormDataActivity(Instance instance, DataElement dataElement)
    {
        var activity = ActivitySource.StartActivity(TraceNameValidateFormData);

        activity.SetInstanceId(instance);
        activity.SetDataElementId(dataElement);
        return activity;
    }

    internal Activity? StartRunFormDataValidatorActivity(IFormDataValidator validator)
    {
        var activity = ActivitySource.StartActivity(TraceNameRunFormDataValidator);

        if (activity is not null)
        {
            activity.SetTag(LabelValidatorType, validator.GetType().Name);
            activity.SetTag(LabelValidatorSource, validator.ValidationSource);
        }

        return activity;
    }

    internal static class Validation
    {
        private const string _prefix = "Validation";

        internal const string TraceNameRunTaskValidator = $"{_prefix}.RunTaskValidator";
        internal const string TraceNameValidateInstanceAtTask = $"{_prefix}.ValidateInstanceAtTask";
        internal const string TraceNameValidateDataElement = $"{_prefix}.ValidateDataElement";
        internal const string TraceNameRunDataElementValidator = $"{_prefix}.RunDataElementValidator";
        internal const string TraceNameValidateFormData = $"{_prefix}.ValidateFormData";
        internal const string TraceNameRunFormDataValidator = $"{_prefix}.RunFormDataValidator";

        internal const string LabelValidatorType = "validator.type";
        internal const string LabelValidatorSource = "validator.source";
    }
}
