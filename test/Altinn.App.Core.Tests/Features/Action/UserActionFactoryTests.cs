#nullable enable
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Models.UserAction;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Action;

public class UserActionFactoryTests
{
    [Fact]
    public void GetActionHandlerOrDefault_should_return_DummyActionHandler_for_id_dummy()
    {
        var factory = new UserActionFactory(new List<IUserAction>() { new DummyUserAction() });

        IUserAction? userAction = factory.GetActionHandlerOrDefault("dummy");

        userAction.Should().NotBeNull();
        userAction.Should().BeOfType<DummyUserAction>();
        userAction!.Id.Should().Be("dummy");
    }
    
    [Fact]
    public void GetActionHandlerOrDefault_should_return_first_DummyActionHandler_for_id_dummy_if_multiple()
    {
        var factory = new UserActionFactory(new List<IUserAction>() { new DummyUserAction(), new DummyUserAction2() });

        IUserAction? userAction = factory.GetActionHandlerOrDefault("dummy");

        userAction.Should().NotBeNull();
        userAction.Should().BeOfType<DummyUserAction>();
        userAction!.Id.Should().Be("dummy");
    }
    
    [Fact]
    public void GetActionHandlerOrDefault_should_return_null_if_id_not_found_and_default_not_set()
    {
        var factory = new UserActionFactory(new List<IUserAction>() { new DummyUserAction() });

        IUserAction? userAction = factory.GetActionHandlerOrDefault("nonexisting");

        userAction.Should().BeNull();
    }
    
    [Fact]
    public void GetActionHandlerOrDefault_should_return_null_if_id_is_null_and_default_not_set()
    {
        var factory = new UserActionFactory(new List<IUserAction>() { new DummyUserAction() });

        IUserAction? userAction = factory.GetActionHandlerOrDefault(null);

        userAction.Should().BeNull();
    }
    
    [Fact]
    public void GetActionHandlerOrDefault_should_return_NullActionHandler_if_id_not_found_and_default_set()
    {
        var factory = new UserActionFactory(new List<IUserAction>() { new DummyUserAction() });

        IUserAction userAction = factory.GetActionHandlerOrDefault("nonexisting", new NullUserAction());

        userAction.Should().BeOfType<NullUserAction>();
        userAction.Id.Should().Be("null");
    }
    
    [Fact]
    public void GetActionHandlerOrDefault_should_return_NullActionHandler_if_id_is_null_and_default_set()
    {
        var factory = new UserActionFactory(new List<IUserAction>() { new DummyUserAction() });

        IUserAction userAction = factory.GetActionHandlerOrDefault(null, new NullUserAction());

        userAction.Should().BeOfType<NullUserAction>();
        userAction.Id.Should().Be("null");
    }
    
    internal class DummyUserAction : IUserAction
    {
        public string Id { get; set; } = "dummy";

        public Task<UserActionResult> HandleAction(UserActionContext context)
        {
            return Task.FromResult(UserActionResult.SuccessResult());
        }
    }
    
    internal class DummyUserAction2 : IUserAction
    {
        public string Id { get; set; } = "dummy";

        public Task<UserActionResult> HandleAction(UserActionContext context)
        {
            return Task.FromResult(UserActionResult.SuccessResult());
        }
    }
}