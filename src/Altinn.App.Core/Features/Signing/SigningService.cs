using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Sign;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningService(ISigneeLogic signeeLogic)
{

    internal void AssignSignees()
    {
        List<SigneeState> state = /*StorageClient.GetSignState ??*/ [];
        signeeLogic.Execute(); // CQRS is probably overkill
        foreach(Signee signee in signeeLogic.GetSignees())
        {
            SigneeState signeeState = state.FirstOrDefault(s => s.Id == signee.UserId) ?? new SigneeState(id: signee.UserId, displayName: /*TODO: get name of org/person*/"");
            try
            {
                if(signeeState.IsDelegated is false)
                {
                    //TODO: delegateSignAction
                    signeeState.IsDelegated = true;
                }

                if(signeeState.IsNotified is false)
                {
                    //TODO: sendCorrespondance
                    signeeState.IsNotified = true;
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
        }
        // TODO: StorageClient.SetSignState(state);
        throw new NotImplementedException();
    }

    internal List<Signee> ReadSignees()
    {
        // TODO: Get signees from state

        // TODO: Get signees from policy

        throw new NotImplementedException();
    }

    internal void Sign(Signee signee)
    {
        // var state = StorageClient.GetSignState(...);
        try
        {
            // SigneeState signeeState = state.FirstOrDefault(s => s.Id == signee.UserId)
            // if(signeeState.hasSigned is false)
            // {
            //      signClient.SignDataElements(...);
            //      signeeState.hasSigned = true;
            // }
            // if(signeeState.IsTaskOwnerNotified is false)
            // {
            //      correspondanceClient.SendMessage(...);
            //      signeeState.IsTaskOwnerNotified = true;
            // }
        }
        catch
        {
            // TODO: log + telemetry?
        }
        finally
        {
            // StorageClient.SetSignState(state);
        }
        throw new NotImplementedException();
    }
}
