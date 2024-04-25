using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Core.Features.Telemetry.Validation;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    private void InitValidation() { }

    internal Activity? StartValidateInstanceAtTaskActivity(
        Platform.Storage.Interface.Models.Instance instance,
        string taskId
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        var activity = ActivitySource.StartActivity(TraceNameValidateInstanceAtTask);
        if (activity is not null)
        {
            activity.SetTag(Labels.TaskId, taskId);

            TryAddInstanceId(activity, instance);
        }
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

    internal Activity? StartValidateDataElementActivity(
        Platform.Storage.Interface.Models.Instance instance,
        DataElement dataElement
    )
    {
        var activity = ActivitySource.StartActivity(TraceNameValidateDataElement);
        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
            TryAddDataElementId(activity, dataElement);
        }
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

    internal Activity? StartValidateFormDataActivity(
        Platform.Storage.Interface.Models.Instance instance,
        DataElement dataElement
    )
    {
        var activity = ActivitySource.StartActivity(TraceNameValidateFormData);

        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
            TryAddDataElementId(activity, dataElement);
        }
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

    private static void TryAddInstanceId(Activity activity, Platform.Storage.Interface.Models.Instance? instance)
    {
        if (instance?.Id is not null)
        {
            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
        }
    }

    private static void TryAddDataElementId(Activity activity, DataElement? dataElement)
    {
        if (dataElement?.Id is not null)
        {
            Guid dataGuid = Guid.Parse(dataElement.Id);
            activity.SetTag(Labels.DataGuid, dataGuid);
        }
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
