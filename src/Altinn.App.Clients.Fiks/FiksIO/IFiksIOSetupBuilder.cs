using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.FiksIO;

public interface IFiksIOSetupBuilder
{
    IFiksIOSetupBuilder WithConfig(Action<FiksIOSettings> configureOptions);
    IFiksIOSetupBuilder WithConfig(string configSectionPath);
    IServiceCollection CompleteSetup();
}
