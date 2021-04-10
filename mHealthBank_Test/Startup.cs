using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SAGE.Core.Commons;
using SAGE.IoC;
using System.Collections.Generic;
using System.Reflection;

namespace mHealthBank
{
    public class Startup
    {
        private SAGE.Core.Interface.ILogger Log;

        public Startup(IConfiguration configuration, IWebHostEnvironment appEnv)
        {
            Configuration = configuration;
            Environment = appEnv;

            Log = mHealthBankLogFactory.GetLogger<Startup>();

            if (appEnv.IsDevelopment())
            {

            }
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews()
                .AddNewtonsoftJson(opt =>
                {
                    //opt.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    //opt.SerializerSettings.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore;
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    opt.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    opt.SerializerSettings.ContractResolver = new Resolver();

                    opt.UseCamelCasing(true);
                });

            services.AddControllers()
                .AddNewtonsoftJson(opt =>
                {
                    //opt.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    //opt.SerializerSettings.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore;
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    opt.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    opt.SerializerSettings.ContractResolver = new Resolver();

                    opt.UseCamelCasing(true);
                });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.RegisterConfiguration(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.CreateActivator();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public void ConfigureContainer(IBuilderIoC builder)
        {
            if (Environment.IsDevelopment())
            {
                builder.RegisterRequiredDevelopment(Configuration);
            }
            else
            {
                builder.RegisterRequired(Configuration);
            }
        }

        class Resolver : DefaultContractResolver
        {
            private readonly IDictionary<string, ICollection<string>> dicIgnores;

            public Resolver()
            {
                dicIgnores = new Dictionary<string, ICollection<string>>();
                PropertyToIgnore();
            }

            private void PropertyToIgnore()
            {
                var col = new HashSet<string>();
                col.Add("Url");
                //col.Add("TotalCount");
                dicIgnores["Microsoft.AspNetCore.Mvc.JsonObject"] = col;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProp = base.CreateProperty(member, memberSerialization);

                var key = member.DeclaringType?.FullName;

                if (!key.IsNullOrEmpty())
                {
                    if (dicIgnores.ContainsKey(key))
                    {
                        var cols = dicIgnores[key];
                        if (cols.Contains(member.Name))
                            jsonProp.Ignored = true;
                    }
                }
                return jsonProp;
            }
        }
    }
}
