using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DoyamoyeeMondir.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        services.AddAutoMapper(
            configuration => { },
            typeof(DependencyInjection).Assembly);

        return services;
    }
}