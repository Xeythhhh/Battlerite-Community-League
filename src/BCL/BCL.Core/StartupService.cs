using Microsoft.Extensions.DependencyInjection;

namespace BCL.Core;

public static class StartupService
{
    /// <summary>
    /// Registers Core services to DI
    /// </summary>
    /// <param name="services">App services</param>
    public static void AddCoreServices(this IServiceCollection services)
    {
        Type[] assemblyTypes = CoreAssembly.Value.GetTypes();
        SetupCoreServices(assemblyTypes, services);
    }

    static void SetupCoreServices(IEnumerable<Type> assemblyTypes, IServiceCollection services)
    {
        IEnumerable<Type> serviceTypes = assemblyTypes
            .Where(x => x.Name.EndsWith("Service") && !x.IsAbstract);

        foreach (Type? implementationType in serviceTypes)
        {
            services.AddScoped(implementationType);
            foreach (Type serviceType in implementationType.GetInterfaces())
                services.AddScoped(serviceType, implementationType);
        }
    }
}
