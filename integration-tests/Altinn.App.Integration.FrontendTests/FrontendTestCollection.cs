namespace Altinn.App.Integration.FrontendTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class FrontendTestFixture : RunningAppFixture
{
    public FrontendTestFixture(IMessageSink messageSink)
        : base("frontend-test", messageSink) { }
}

[CollectionDefinition("FrontendTestCollection")]
public class FrontendTestCollection : ICollectionFixture<FrontendTestFixture>;
