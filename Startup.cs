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
            optionsBuilder.UseMySql("server=localhost;UserId=root;Password=m1llions;database=medalynx_db;");
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
            /*
            services.AddDbContextPool<MedialynxDbContext>(options => options
                // replace with your connection string
                .UseMySql("Server=localhost:3306;Database=ef;User=root;Password=m1llions;", mySqlOptions => mySqlOptions
                    // replace with your Server Version and Type
                    .ServerVersion(new ServerVersion(new Version(8, 0, 18), ServerType.MySql))
            ));
            */
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

                endpoints.MapPost("/adduser", context => userApi.AddUser(context));
                endpoints.MapPost("/updateuser", context => userApi.UpdateUser(context));
                endpoints.MapDelete("/removeuser", context => userApi.RemoveUser(context));
            });
        }

        public void ConfigureOLD(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var serverAddressesFeature =
                app.ServerFeatures.Get<IServerAddressesFeature>();

            app.UseStaticFiles();

            app.Run(async (context) =>
            {
                /*
                context.Response.ContentType = "text/html";
                await context.Response
                    .WriteAsync("<!DOCTYPE html><html lang=\"en\"><head>" +
                        "<title></title></head><body><p>Hosted by Kestrel</p>");
                */
                if (serverAddressesFeature != null)
                {
                    await context.Response
                        .WriteAsync("<p>Listening on the following addresses: " +
                            string.Join(", ", serverAddressesFeature.Addresses) +
                            "</p>");
                }
                
                System.Console.Write("requesr recivied\r\n");
                await context.Response.WriteAsync("<p>Request URL: " +
                    $"{context.Request.GetDisplayUrl()}<p>");
            });
        }
    }
}
