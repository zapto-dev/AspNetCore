namespace Zapto.AspNetCore.NetFx.Tests;

public class ResponseBodyFeatureTest(AspNetFixture fixture)
{
    [Fact]
    public async Task SendFile()
    {
        using var response = await fixture.Client.GetAsync("/file", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal("Hello world", content);
    }
}
