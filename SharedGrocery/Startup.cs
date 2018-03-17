﻿﻿using System;
 using System.Text;
 using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
 using Microsoft.IdentityModel.Tokens;
 using SharedGrocery.GroceryService.Repository.DBContexts;
 using SharedGrocery.Uaa.Repository;
 using SharedGrocery.Uaa.Repository.DBContext;
using SharedGrocery.Uaa.Service;

namespace SharedGrocery
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Startup
    {
        private ILogger<Startup> _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // ReSharper disable once UnusedMember.Global
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddEntityFrameworkNpgsql()
                .AddDbContext<GroceryDataContext>(opt =>
                    opt.UseNpgsql(Configuration.GetConnectionString("Groceries")));

            services.AddEntityFrameworkNpgsql()
                .AddDbContext<UaaContext>(opt =>
                    opt.UseNpgsql(Configuration.GetConnectionString("Uaa")));

            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "robindegier.nl",
                        ValidAudience = "robindegier.nl",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(Configuration["ApiSecret"]))
                    };
                });

            var containerBuilder = new ContainerBuilder();
            
            containerBuilder.Populate(services);
            containerBuilder.RegisterType<UserService>().As<IUserService>();
            containerBuilder.RegisterType<AuthenticationService>().As<IAuthenticationService>();
            containerBuilder.RegisterType<UserRepository>().As<IUserRepository>();
            
            var loggingFactory = new LoggerFactory();
            loggingFactory.AddConsole(Configuration.GetSection("Logging"));
            loggingFactory.AddDebug();
            containerBuilder.RegisterInstance(loggingFactory).As<ILoggerFactory>();

            var container = containerBuilder.Build();
            return container.Resolve<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // ReSharper disable once UnusedMember.Global
        public void Configure(IApplicationBuilder app, GroceryDataContext groceryDataContext, UaaContext uaaContext,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
            
            _logger?.LogDebug("Starting database migration");
            groceryDataContext.Database.Migrate();
            uaaContext.Database.Migrate();

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}