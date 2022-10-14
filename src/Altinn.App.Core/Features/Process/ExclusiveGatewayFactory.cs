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

    public IProcessExclusiveGateway GetProcessExclusiveGateway(string gatewayId)
    {
        foreach (var gateway in _gateways)
        {
            if (String.Equals(gateway.GatewayId, gatewayId, StringComparison.CurrentCultureIgnoreCase))
            {
                return gateway;
            }
        }

        if (String.Equals(DefaultImplName, gatewayId, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new KeyNotFoundException("No default IProcessExclusiveGateway implementation found. Please check your service configuration.");
        }   
        
        var defaultExclusiveGateway = (DefaultExclusiveGateway)GetProcessExclusiveGateway(DefaultImplName);
        return defaultExclusiveGateway.CloneDefaultTo(gatewayId);
    }
}
