using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlobStorageApp.Settings;
using BlobStorageApp.Repositories;

namespace BlobStorageApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Setup swagger 
            SetupSwaggerDocuments(services);

            // Configure Settings, this approach is easier than the IOptions for consumers            
            // Singleton indicates an the same instance is used for every request
            // This is essentially caching the settings for the app service.
            services.AddSingleton(CreateStorageAccountSettings);
            services.AddSingleton(CreatePictureSettings);

            // Configure Repositories
            // Scoped indicates an the instance is the same instance for the request but different across requests            
            services.AddScoped(typeof(IStorageRepository), typeof(StorageRepository));
        }

        /// <summary>
        /// Creates the storage account settings
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private IStorageAccountSettings CreateStorageAccountSettings(IServiceProvider arg)
        {
            return Configuration.GetSection(nameof(StorageAccountSettings)).Get<StorageAccountSettings>();
        }

        /// <summary>
        /// Creates the picture settings
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private IPictureSettings CreatePictureSettings(IServiceProvider arg)
        {
            return Configuration.GetSection(nameof(PictureSettings)).Get<PictureSettings>();
        }

        /// <summary>
        /// Sets up the swagger documents
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void SetupSwaggerDocuments(IServiceCollection services)
        {
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Task List API",
                    Version = "v1",
                    Description = "Task List API",
                });

                // Use method name as operationId so that ADD REST Client... will work
                c.CustomOperationIds(apiDesc =>
                {
                    return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null;
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            SetupSwaggerJsonGenerationAndUI(app);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// Sets up the Swagger JSON file and Swagger Interactive UI
        /// </summary>
        /// <param name="app">The application builder</param>
        private static void SetupSwaggerJsonGenerationAndUI(IApplicationBuilder app)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                // Use the older 2.0 format so the ADD REST Client... will work
                c.SerializeAsV2 = true;
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            //       specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task List API");

                // Serve the Swagger UI at the app's root (http://localhost:<port>)
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
