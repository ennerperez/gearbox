using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Runner
{
    [ExcludeFromCodeCoverage]
    public static class Extensions
    {
        public static IServiceCollection AddRunner(this IServiceCollection serviceCollection)
        {
            return serviceCollection;
        }
    }
}
