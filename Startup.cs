using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;

namespace Medalynx
{
    public class MedialynxDbContext : DbContext {
        public DbSet<User> Users { get; set; }
 
        public MedialynxDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=35.188.34.140;UserId=root;Password=m1llions;database=medalynx_db;");
        }
    }

    public class Startup
    {
        private Api.UserApi userApi = new Api.UserApi();
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            System.Console.Write("ConfigureServices called...\r\n");
            services.Configure<KestrelServerOptions>(
                Configuration.GetSection("Kestrel"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();

            // Matches request to an endpoint.
            app.UseRouting();

            // Execute the matched endpoint.
            app.UseEndpoints(endpoints =>
            {
                // Configuration of app endpoints.
                endpoints.MapGet("/", context => context.Response.WriteAsync("Hello world"));
                endpoints.MapGet("/test", context => context.Response.WriteAsync("test callback"));
                endpoints.MapPost("/posttest", context => context.Response.WriteAsync("test callback post"));

                endpoints.MapGet("/user", context => userApi.GetUser(context));
                endpoints.MapPost("/adduser", context => userApi.AddUser(context));
                endpoints.MapPost("/updateuser", context => userApi.UpdateUser(context));
                endpoints.MapDelete("/removeuser", context => userApi.RemoveUser(context));

                endpoints.MapPost("/test", context => userApi.Test(context));
            });
        }
    }
}
