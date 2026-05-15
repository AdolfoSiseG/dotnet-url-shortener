namespace UrlShortener.Api.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Integration";
}
