using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Core.Features.Telemetry.Validation;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    private void InitValidation()
    {
    }

    internal Activity? StartValidateInstanceAtTaskActivity(Platform.Storage.Interface.Models.Instance instance, string taskId)
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

    internal Activity? StartRunTaskValidatorActivity(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        var activity = ActivitySource.StartActivity(TraceNameRunTaskValidator);
        activity?.SetTag(Labels.TaskId, taskId);
        return activity;
    }

    internal Activity? StartValidateDataElementActivity(Platform.Storage.Interface.Models.Instance instance, DataElement dataElement)
    {
        var activity = ActivitySource.StartActivity(TraceNameValidateDataElement);
        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
            TryAddDataElementId(activity, dataElement);
        }
        return activity;
    }

    internal Activity? StartRunDataElementValidatorActivity(string dataType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        var activity = ActivitySource.StartActivity(TraceNameRunDataElementValidator);
        return activity;
    }

    internal Activity? StartValidateFormDataActivity(Platform.Storage.Interface.Models.Instance instance, DataElement dataElement)
    {
        var activity = ActivitySource.StartActivity(TraceNameValidateFormData);
        
        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
            TryAddDataElementId(activity, dataElement);
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

    internal Activity? StartRunFormDataValidatorActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameRunFormDataValidator);
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
    }
}