// using Docker.DotNet.Models;
// using DotNet.Testcontainers.Builders;
// using DotNet.Testcontainers.Configurations;
// using DotNet.Testcontainers.Containers;
//
// public class LibraryFrontendFixture: Testcontainers.Xunit.ContainerFixture<ContainerBuilder,DockerContainer>
// {
//
// }
//
// public class LocaltestBuilder : ContainerBuilder<LocaltestBuilder, IContainer, IContainerConfiguration>
// {
//     public LocaltestBuilder()
//     {
//         WithImage("altinn/localtest:latest");
//         WithEnvironment("DOTNET_ENVIRONMENT", "Docker");
//         WithEnvironment("LocalPlatformSettings__LocalAppUrl", "http://app:5005");
//     }
//
//     public override IContainer Build()
//     {
//         throw new NotImplementedException();
//     }
//
//     protected override LocaltestBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
//     {
//         throw new NotImplementedException();
//     }
//
//     protected override LocaltestBuilder Merge(IContainerConfiguration oldValue, IContainerConfiguration newValue)
//     {
//         throw new NotImplementedException();
//     }
//
//     protected override IContainerConfiguration DockerResourceConfiguration { get; }
//     protected override LocaltestBuilder Clone(IContainerConfiguration resourceConfiguration)
//     {
//         throw new NotImplementedException();
//     }
// }
