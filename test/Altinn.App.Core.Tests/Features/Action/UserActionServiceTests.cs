using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Internal;
using Altinn.App.Core.Internal.Exceptions;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Action;

public class UserActionServiceTests
{
    [Fact]
    public async Task HandleAction_throws_NotFoundException_if_no_handler_found()
    {
        UserActionService userActionService = new UserActionService(new UserActionFactory(new List<IUserAction>()));
        Func<Task> act = async () => { await userActionService.HandleAction(new UserActionContext(new Instance(), 0), "notFound"); };
        await act.Should().ThrowAsync<NotFoundException>();
    }
    
    [Fact]
    public async Task HandleAction_returns_result_with_validationgroup_set_to_type_as_validation_grouping_key()
    {
        UserActionService userActionService = new UserActionService(new UserActionFactory(new List<IUserAction>() { new DummyUserAction(), new DummyUserActionWithValidationGroup() }));
        var expected = new UserActionServiceResult(UserActionResult.SuccessResult(), typeof(DummyUserAction).ToString());
        var actual = await userActionService.HandleAction(new UserActionContext(new Instance(), 0), "dummy");
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public async Task HandleAction_returns_result_with_validationgroup_set_to_value_of_ValidationGroup_when_set()
    {
        UserActionService userActionService = new UserActionService(new UserActionFactory(new List<IUserAction>() { new DummyUserAction(), new DummyUserActionWithValidationGroup() }));
        var expected = new UserActionServiceResult(UserActionResult.SuccessResult(), "MyCustomGroup");
        var actual = await userActionService.HandleAction(new UserActionContext(new Instance(), 0), "dummyWithGroup");
        actual.Should().BeEquivalentTo(expected);
    }
    
    private class DummyUserAction : IUserAction
    {
        public string Id => "dummy";

        public Task<UserActionResult> HandleAction(UserActionContext context)
        {
            return Task.FromResult(UserActionResult.SuccessResult());
        }
    }

    private class DummyUserActionWithValidationGroup : IUserAction
    {
        public string Id => "dummyWithGroup";

        public string ValidationGroup => "MyCustomGroup";

        public Task<UserActionResult> HandleAction(UserActionContext context)
        {
            return Task.FromResult(UserActionResult.SuccessResult());
        }
    }
}