namespace Altinn.App.Core.Features.Process;

public class ExclusiveGatewayFactory
{
    /// <summary>
    /// Name of the default logic for exclusive gateways
    /// </summary>
    public const string DefaultImplName = "altinn_default_gateway";
    private readonly IEnumerable<IProcessExclusiveGateway> _gateways;

    public ExclusiveGatewayFactory(IEnumerable<IProcessExclusiveGateway> gateways)
    {
        _gateways = gateways;
    }

    public IProcessExclusiveGateway? GetProcessExclusiveGateway(string gatewayId)
    {
        foreach (var gateway in _gateways)
        {
            if (String.Equals(gateway.GatewayId, gatewayId, StringComparison.CurrentCultureIgnoreCase))
            {
                return gateway;
            }
        }

        return null;
    }
}
