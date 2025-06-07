using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ledger11.tests;

[CollectionDefinition("WebApplicationFactory Collection")]
public class WebApplicationFactoryCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is just to attach [CollectionDefinition].
}

[Collection("WebApplicationFactory Collection")]
public class TestEndpoints : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TestEndpoints(CustomWebApplicationFactory factory)
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true, CookieContainer = new CookieContainer() };
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task GET_Home_Index()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GET_Api_Category()
    {
        // This will cause a application user and space to be assigned for the single user
        var response1 = await _client.GetAsync("/api/category");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
    }

}
