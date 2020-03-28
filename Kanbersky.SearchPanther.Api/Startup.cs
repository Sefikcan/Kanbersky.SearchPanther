using Kanbersky.SearchPanther.Business.Abstract;
using Kanbersky.SearchPanther.Business.Concrete;
using Kanbersky.SearchPanther.Core.Extensions;
using Kanbersky.SearchPanther.Core.Helpers.ElasticSearch;
using Kanbersky.SearchPanther.Core.Setting;
using Kanbersky.SearchPanther.DAL.Concrete.EntityFramework.Context;
using Kanbersky.SearchPanther.DAL.Concrete.EntityFramework.GenericRepository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kanbersky.SearchPanther.Api
{
    public class Startup
    {
        public IConfiguration _configuration { get; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.Configure<ElasticSearchSettings>(_configuration.GetSection("ElasticSearchSettings"));
            services.AddSingleton(typeof(ElasticClientProvider));

            services.AddDbContext<KanberContext>(opt =>
            {
                opt.UseSqlServer(_configuration["ConnectionStrings:DefaultConnection"]);
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IElasticSearchService, ElasticSearchService>();
            services.AddScoped<IProductService, ProductService>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Kanbersky.Search Microservice",
                    Version = "v2"
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseExceptionMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kanbersky Search V2");
            });
        }
    }
}
