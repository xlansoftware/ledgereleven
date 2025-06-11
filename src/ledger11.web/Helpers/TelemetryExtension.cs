using ledger11.model.Data;
using ledger11.service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

public static class TelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetry(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        var env = builder.Environment;

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("AspNetOtel");

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusExporter()
                    ;
            })
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = "localhost";
                        options.AgentPort = 6831;
                    })
                    ;
            });


        return services;
    }
}