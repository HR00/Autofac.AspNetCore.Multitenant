﻿using System;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sandbox
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public static MultitenantContainer ApplicationContainer { get; set; }

        public IConfigurationRoot Configuration { get; }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterType<Dependency>()
                .As<IDependency>()
                .WithProperty("Id", "base");
            var container = builder.Build();
            var strategy = new QueryStringTenantIdentificationStrategy(container.Resolve<IHttpContextAccessor>(), container.Resolve<ILogger<QueryStringTenantIdentificationStrategy>>());
            var mtc = new MultitenantContainer(strategy, container);
            mtc.ConfigureTenant(
                "a",
                cb => cb
                        .RegisterType<Dependency>()
                        .As<IDependency>()
                        .WithProperty("Id", "a"));
            mtc.ConfigureTenant(
                "b",
                cb => cb
                        .RegisterType<Dependency>()
                        .As<IDependency>()
                        .WithProperty("Id", "b"));
            Startup.ApplicationContainer = mtc;
            return new AutofacServiceProvider(mtc);
        }
    }
}
