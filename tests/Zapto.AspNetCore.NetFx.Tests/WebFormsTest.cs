namespace Zapto.AspNetCore.NetFx.Tests;

public class WebFormsTest(AspNetFixture fixture)
{
    [Fact]
    public async Task WebFormsPage_Accessible()
    {
        using var response = await fixture.Client.GetAsync("/WebForms.aspx", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Hello from WebForms", content);
    }
}
