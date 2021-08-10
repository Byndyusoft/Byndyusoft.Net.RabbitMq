using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Byndyusoft.Net.RabbitMq.Extensions
{
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the services from.</param>
        /// <param name="allowedTypes"></param>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public static IEnumerable<T> GetServices<T>(this IServiceProvider provider, IList<Type> allowedTypes)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var result = provider.GetRequiredService<IEnumerable<T>>();
            foreach (var service in result)
            {
                if (allowedTypes.Any(t => t == service?.GetType()))
                    yield return service;
            }
        }
    }
}