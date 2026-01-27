using Altinn.App.Core.Internal.ProcessEngine;
using Altinn.App.Core.Internal.ProcessEngine.Commands;
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
        services.AddTransient<IProcessEngineCommand, UpdateProcessState>();

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
        services.AddTransient<IProcessEngineCommand, UnlockTaskData>();
        services.AddTransient<IProcessEngineCommand, ProcessTaskStartLegacyHook>();
        services.AddTransient<IProcessEngineCommand, OnTaskStartingHook>();
        services.AddTransient<IProcessEngineCommand, CommonTaskInitialization>();
        services.AddTransient<IProcessEngineCommand, ProcessTaskStart>();
        services.AddTransient<IProcessEngineCommand, MovedToAltinnEvent>();
        services.AddTransient<IProcessEngineCommand, InstanceCreatedAltinnEvent>();
        services.AddTransient<IProcessEngineCommand, ExecuteServiceTask>();
        services.AddTransient<IProcessEngineCommand, ProcessTaskEnd>();
        services.AddTransient<IProcessEngineCommand, CommonTaskFinalization>();
        services.AddTransient<IProcessEngineCommand, EndTaskLegacyHook>();
        services.AddTransient<IProcessEngineCommand, OnTaskEndingHook>();
        services.AddTransient<IProcessEngineCommand, LockTaskData>();
        services.AddTransient<IProcessEngineCommand, ProcessTaskAbandon>();
        services.AddTransient<IProcessEngineCommand, OnTaskAbandonHook>();
        services.AddTransient<IProcessEngineCommand, AbandonTaskLegacyHook>();
        services.AddTransient<IProcessEngineCommand, OnProcessEndingHook>();
        services.AddTransient<IProcessEngineCommand, ProcessEndLegacyHook>();
        services.AddTransient<IProcessEngineCommand, CompletedAltinnEvent>();
        services.AddTransient<IProcessEngineCommand, UpdateProcessState>();
    }
}
