using System.Net.Http;

namespace Zapto.AspNetCore.NetFx.Tests;

[CollectionDefinition(nameof(CounterCollectionDefinition), DisableParallelization = true)]
public class CounterCollectionDefinition;

[Collection(nameof(CounterCollectionDefinition))]
public class ModuleTest(AspNetFixture fixture)
{
    [Theory]
    [InlineData("handler", "handler")]
    [InlineData("module", "module")]
    [InlineData("handler", "module")]
    [InlineData("module", "handler")]
    public async Task Mixed_Mode(string prefix, string secondPrefix)
    {
        // Test the counter service via two different endpoints to ensure singleton behavior
        using (var response = await fixture.Client.PostAsync(
                   $"{prefix}/counter/reset",
                   new MultipartFormDataContent(),
                   TestContext.Current.CancellationToken))
        {
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("Counter reset.", content);
        }

        using (var response = await fixture.Client.PostAsync(
                   $"{prefix}/counter/increment",
                   new MultipartFormDataContent(),
                   TestContext.Current.CancellationToken))
        {
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("Count: 1", content);
        }

        using (var response = await fixture.Client.PostAsync(
                   $"{secondPrefix}/counter/increment",
                   new MultipartFormDataContent(),
                   TestContext.Current.CancellationToken))
        {
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("Count: 2", content);
        }
    }
}
