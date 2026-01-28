using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.AltinnEvents;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.ProcessEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskAbandon;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskStart;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Tests.Internal.ProcessEngine;

public class ProcessEngineCommandValidatorTests
{
    [Fact]
    public void Validate_AllCommandsRegistered_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterAllCommands(services);

        // Act & Assert - should not throw
        ProcessEngineCommandValidator.Validate(services);
    }

    [Fact]
    public void Validate_MissingCommand_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Intentionally NOT registering all commands
        services.AddTransient<IWorkflowEngineCommand, UpdateProcessStateInStorage>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProcessEngineCommandValidator.Validate(services)
        );

        Assert.Contains("not registered", exception.Message);
        Assert.Contains("OnTaskStartingHook", exception.Message);
    }

    private static void RegisterAllCommands(IServiceCollection services)
    {
        // Register all commands that are referenced in ProcessEventCommands
        services.AddTransient<IWorkflowEngineCommand, UnlockTaskData>();
        services.AddTransient<IWorkflowEngineCommand, WorkflowTaskStartLegacyHook>();
        services.AddTransient<IWorkflowEngineCommand, OnTaskStartingHook>();
        services.AddTransient<IWorkflowEngineCommand, CommonTaskInitialization>();
        services.AddTransient<IWorkflowEngineCommand, WorkflowTaskStart>();
        services.AddTransient<IWorkflowEngineCommand, MovedToAltinnEvent>();
        services.AddTransient<IWorkflowEngineCommand, InstanceCreatedAltinnEvent>();
        services.AddTransient<IWorkflowEngineCommand, ExecuteServiceTask>();
        services.AddTransient<IWorkflowEngineCommand, WorkflowTaskEnd>();
        services.AddTransient<IWorkflowEngineCommand, CommonTaskFinalization>();
        services.AddTransient<IWorkflowEngineCommand, EndTaskLegacyHook>();
        services.AddTransient<IWorkflowEngineCommand, OnTaskEndingHook>();
        services.AddTransient<IWorkflowEngineCommand, LockTaskData>();
        services.AddTransient<IWorkflowEngineCommand, WorkflowTaskAbandon>();
        services.AddTransient<IWorkflowEngineCommand, OnTaskAbandonHook>();
        services.AddTransient<IWorkflowEngineCommand, AbandonTaskLegacyHook>();
        services.AddTransient<IWorkflowEngineCommand, OnWorkflowEndingHook>();
        services.AddTransient<IWorkflowEngineCommand, WorkflowEndLegacyHook>();
        services.AddTransient<IWorkflowEngineCommand, CompletedAltinnEvent>();
        services.AddTransient<IWorkflowEngineCommand, UpdateProcessStateInStorage>();
    }
}
