using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Configures forwarded headers with default settings for common proxy scenarios
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="additionalKnownNetworks">Optional additional trusted networks</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseDefaultForwardedHeaders(
        this IApplicationBuilder app,
        params IPNetwork[] additionalKnownNetworks)
    {
        var options = new ForwardedHeadersOptions
        {
            // Specify which headers should be processed:
            // - X-Forwarded-For: Contains the original client IP address
            // - X-Forwarded-Proto: Contains the original scheme (http/https)
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Forward limit helps prevent header spoofing
            ForwardLimit = 2, // Cloudflare -> Nginx -> Kestrel
        };

        // Add default private network ranges (RFC 1918)
        // When hosted in a docker compose environment, the proxy is on the local network
        // and we need to trust the forwarded headers from it.
        options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
        options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("192.168.0.0"), 16));

        // Add any additional trusted networks provided by the caller
        foreach (var network in additionalKnownNetworks)
        {
            options.KnownNetworks.Add(network);
        }

        // TODO: Read from configuration?
        return app.UseForwardedHeaders(options);
    }
}