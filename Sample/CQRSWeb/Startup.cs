﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CQRSlite.Commands;
using CQRSlite.Events;
using CQRSlite.Domain;
using CQRSCode.WriteModel;
using CQRSlite.Caching;
using CQRSCode.ReadModel;
using CQRSCode.WriteModel.Handlers;
using System.Reflection;
using System.Linq;
using CQRSlite.Messages;
using CQRSlite.Routing;

namespace CQRSWeb
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            //Add Cqrs services
            services.AddSingleton<Router>(new Router());
            services.AddSingleton<ICommandSender>(y => y.GetService<Router>());
            services.AddSingleton<IEventPublisher>(y => y.GetService<Router>());
            services.AddSingleton<IHandlerRegistrar>(y => y.GetService<Router>());
            services.AddScoped<ISession, Session>();
            services.AddSingleton<IEventStore, InMemoryEventStore>();
            services.AddScoped<ICache, MemoryCache>();
            services.AddScoped<IRepository>(y => new CacheRepository(new Repository(y.GetService<IEventStore>()), y.GetService<IEventStore>(), y.GetService<ICache>()));

            services.AddTransient<IReadModelFacade, ReadModelFacade>();

            //Scan for commandhandlers and eventhandlers
            services.Scan(scan => scan
                .FromAssemblies(typeof(InventoryCommandHandlers).GetTypeInfo().Assembly)
                    .AddClasses(classes => classes.Where(x => {
                        var allInterfaces = x.GetInterfaces();
                        return
                            allInterfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(IHandler<>)) ||
                            allInterfaces.Any(y => y.GetTypeInfo().IsGenericType && y.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICancellableHandler<>));
                    }))
                    .AsSelf()
                    .WithTransientLifetime()
            );

            // Add framework services.
            services.AddMvc();

            //Register router
            var serviceProvider = services.BuildServiceProvider();
            var registrar = new RouteRegistrar(new DependencyResolver(serviceProvider));
            registrar.Register(typeof(InventoryCommandHandlers));

            return serviceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
