namespace CleanArchitecture.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<WebApplicationFactoryFixture>
{
    public const string Name = "Integration";
}
