using Altinn.App.Core.EFormidling.Interface;
using Altinn.Common.EFormidlingClient.Models.SBD;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.EFormidling.Implementation;

/// <summary>
/// Default implementation of <see cref="Altinn.App.Core.EFormidling.Interface.IEFormidlingReceivers"/>
/// </summary>
public class DefaultEFormidlingReceivers : IEFormidlingReceivers
{
    /// <inheritdoc />
    public Task<List<Receiver>> GetEFormidlingReceivers(Instance instance, string? receiverFromConfig = null)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (string.IsNullOrWhiteSpace(receiverFromConfig))
        {
            return Task.FromResult(new List<Receiver>());
        }

        var identifier = new Identifier
        {
            // 0192 prefix for all Norwegian organisations.
            Value = $"0192:{receiverFromConfig.Trim()}",
            Authority = "iso6523-actorid-upis",
        };

        var receiver = new Receiver { Identifier = identifier };

        return Task.FromResult<List<Receiver>>([receiver]);
    }
}
