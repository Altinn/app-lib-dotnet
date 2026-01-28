using System.Reflection;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.ProcessEngine.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Validates that all process engine commands referenced in ProcessEventCommands are registered in DI.
/// </summary>
internal static class ProcessEngineCommandValidator
{
    /// <summary>
    /// Validates that all required commands are registered. Throws if any are missing.
    /// Call this immediately after registering commands in AddProcessServices.
    /// </summary>
    public static void Validate(IServiceCollection services)
    {
        HashSet<string> requiredCommandKeys = GetRequiredCommandKeys();
        HashSet<string> registeredCommandKeys = GetRegisteredCommandKeys(services);

        var missingCommands = requiredCommandKeys.Except(registeredCommandKeys).ToList();

        if (missingCommands.Count > 0)
        {
            string missingCommandsList = string.Join(", ", missingCommands.Select(k => $"'{k}'"));
            throw new InvalidOperationException(
                $"Process Engine configuration error: The following command keys are referenced but not registered: {missingCommandsList}. "
                    + "Ensure all commands are registered in ServiceCollectionExtensions.AddProcessServices()."
            );
        }
    }

    private static HashSet<string> GetRequiredCommandKeys()
    {
        var keys = new HashSet<string>();

        // Collect keys from all event types
        CollectCommandKeys(
            WorkflowCommandSet.GetTaskStartSteps(serviceTaskType: null, isInitialTaskStart: false),
            keys
        );
        CollectCommandKeys(WorkflowCommandSet.GetTaskStartSteps(serviceTaskType: null, isInitialTaskStart: true), keys);
        CollectCommandKeys(
            WorkflowCommandSet.GetTaskStartSteps(serviceTaskType: "DummyServiceTask", isInitialTaskStart: false),
            keys
        );
        CollectCommandKeys(WorkflowCommandSet.GetTaskEndSteps(), keys);
        CollectCommandKeys(WorkflowCommandSet.GetTaskAbandonSteps(), keys);
        CollectCommandKeys(WorkflowCommandSet.GetProcessEndSteps(), keys);

        // UpdateProcessState is automatically inserted
        keys.Add(UpdateProcessStateInStorage.Key);

        return keys;
    }

    private static void CollectCommandKeys(WorkflowCommandSet eventCommandSet, HashSet<string> keys)
    {
        foreach (var commandRequest in eventCommandSet.Commands)
        {
            if (commandRequest.Command is ProcessEngineCommand.AppCommand appCommand)
            {
                keys.Add(appCommand.CommandKey);
            }
        }

        foreach (var commandRequest in eventCommandSet.PostProcessNextCommittedCommands)
        {
            if (commandRequest.Command is ProcessEngineCommand.AppCommand appCommand)
            {
                keys.Add(appCommand.CommandKey);
            }
        }
    }

    private static HashSet<string> GetRegisteredCommandKeys(IServiceCollection services)
    {
        return services
            .Where(sd => sd.ServiceType == typeof(IWorkflowEngineCommand))
            .Select(sd => sd.ImplementationType)
            .Where(implType => implType is not null)
            .Select(implType => GetCommandKeyFromType(implType!))
            .ToHashSet();
    }

    private static string GetCommandKeyFromType(Type commandType)
    {
        // Get the static Key property
        var keyProperty = commandType.GetProperty("Key", BindingFlags.Public | BindingFlags.Static);

        if (keyProperty?.PropertyType == typeof(string))
        {
            return (string)keyProperty.GetValue(null)!;
        }

        throw new InvalidOperationException(
            $"Command type {commandType.Name} does not have a public static 'Key' property"
        );
    }
}
