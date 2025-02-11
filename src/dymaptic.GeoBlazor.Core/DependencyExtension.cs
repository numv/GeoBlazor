﻿using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Core.Objects;
using Microsoft.Extensions.DependencyInjection;


namespace dymaptic.GeoBlazor.Core;

/// <summary>
///     Static extension class for injecting GeoBlazor types
/// </summary>
public static class DependencyExtension
{
    /// <summary>
    ///     Adds the Logic components <see cref="GeometryEngine" /> and <see cref="Projection" /> to your dependency
    ///     injection collection.
    /// </summary>
    /// <remarks>
    ///     Since Scoped services behave like singletons in client applications (wasm, maui), registering the OAuthAuthentication
    ///     service as scoped is safe for all implementations.
    ///     https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-7.0#service-lifetime
    /// </remarks>
    public static IServiceCollection AddGeoBlazor(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddScoped<GeometryEngine>()
            .AddScoped<Projection>()
            .AddScoped<AbortManager>()
            .AddScoped<AuthenticationManager>();
    }
}