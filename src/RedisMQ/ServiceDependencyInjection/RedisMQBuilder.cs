// // Copyright (c) .NET Core Community. All rights reserved.
// // Licensed under the MIT License. See License.txt in the project root for license information.
//
// using System;
// using System.Linq;
// using System.Reflection;
// using RedisMQ.Internal;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.DependencyInjection.Extensions;
//
// // ReSharper disable UnusedMember.Global
//
// namespace RedisMQ
// {
//    
//     /// <summary>
//     /// Allows fine grained configuration of RedisMQ services.
//     /// </summary>
//     public sealed class RedisMQBuilder
//     {
//         public RedisMQBuilder(IServiceCollection services)
//         {
//             Services = services;
//         }
//
//         /// <summary>
//         /// Gets the <see cref="IServiceCollection" /> where MVC services are configured.
//         /// </summary>
//         public IServiceCollection Services { get; }
//
//         /// <summary>
//         /// Registers subscribers from the specified assemblies.
//         /// </summary>
//         /// <param name="assemblies">Assemblies to scan subscriber</param>
//         public RedisMQBuilder AddSubscriberAssembly(params Assembly[] assemblies)
//         {
//             if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
//
//             Services.Replace(new ServiceDescriptor(typeof(IConsumerServiceSelector),
//             x => new AssemblyConsumerServiceSelector(x, assemblies),
//             ServiceLifetime.Singleton));
//
//             return this;
//         }
//
//         /// <summary>
//         /// Registers subscribers from the specified types.
//         /// </summary>
//         /// <param name="handlerAssemblyMarkerTypes"></param>
//         public RedisMQBuilder AddSubscriberAssembly(params Type[] handlerAssemblyMarkerTypes)
//         {
//             if (handlerAssemblyMarkerTypes == null) throw new ArgumentNullException(nameof(handlerAssemblyMarkerTypes));
//
//             AddSubscriberAssembly(handlerAssemblyMarkerTypes.Select(t => t.GetTypeInfo().Assembly).ToArray());
//
//             return this;
//         }
//     }
// }