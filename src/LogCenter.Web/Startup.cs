﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Common.Logs;
using LogCenter.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using NLog.Extensions.Logging;

namespace LogCenter.Web
{
    public class Startup
    {
        public IHostingEnvironment HostingEnvironment { get; set; }
        public IConfiguration Configuration { get; set; }
        public IApplicationLifetime ApplicationLifetime { get; set; }

        public Startup(IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            HostingEnvironment = hostingEnvironment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(config =>
            {
                //'Access-Control-Allow-Origin' header in the response must not be the wildcard '*'
                //Access to XMLHttpRequest at 'http://192.168.1.235:8000/hubs/logHub/negotiate' from origin
                //'http://localhost:1635' has been blocked by CORS policy:
                //Response to preflight request doesn't pass access control check:
                //The value of the 'Access - Control - Allow - Origin' header in the response must not be the wildcard ' * '
                //when the request's credentials mode is 'include'.
                //The credentials mode of requests initiated by the XMLHttpRequest is controlled by the withCredentials attribute.

                var corsPolicy = new CorsPolicy();
                corsPolicy.Headers.Add("*");
                corsPolicy.Methods.Add("*");
                //corsPolicy.Origins.Add("*");
                corsPolicy.IsOriginAllowed = host => true;
                corsPolicy.SupportsCredentials = false;
                config.AddPolicy("policy", corsPolicy);
            });

            //json setting
            services.AddMvcCore().AddJsonOptions(options =>
            {
                //options.SerializerSettings.Converters.Add(new StringEnumConverter
                //{
                //    AllowIntegerValues = false,
                //    NamingStrategy = new CamelCaseNamingStrategy(true, true)
                //});
                //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .ConfigureApiBehaviorOptions(options =>
                {
                    //options.SuppressConsumesConstraintForFormFileParameters = true;
                    //options.SuppressInferBindingSourcesForParameters = true;
                    //options.SuppressModelStateInvalidFilter = true;
                    //options.SuppressMapClientErrors = true;
                    //options.ClientErrorMapping[404].Link = "https://httpstatuses.com/404";
                });

            services.AddLogCenterServer();
            services.AddMyNLog();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            ApplicationLifetime = applicationLifetime;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            UseMyStaticFiles(app);

            app.UseCors("policy");
            
            app.UseMvc();
            
            app.UseLogCenterServer();
            app.UseMyNLog();
        }

        private void UseMyStaticFiles(IApplicationBuilder app)
        {
            app.UseDefaultFiles(new DefaultFilesOptions() { DefaultFileNames = new List<string>() { "index.html" } });
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = new FileExtensionContentTypeProvider
                {
                    Mappings = { [".vue"] = "text/html" }
                }
            });

            //make logs directory browser ok:
            var logsFileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs"));
            var logsRequestPath = "/logs";
            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = logsFileProvider,
                RequestPath = logsRequestPath
            });

        }
    }

    public static class NLogBoot
    {
        public static IServiceCollection AddMyNLog(this IServiceCollection services)
        {
            services.AddLogging(config =>
            {
                config.AddNLog("nlog.config"); //for file log
                //config.SetMinimumLevel(LogLevel.Trace);
            });
            return services;
        }

        public static void UseMyNLog(this IApplicationBuilder app)
        {
            var applicationLifetime = app.ApplicationServices.GetService<IApplicationLifetime>();
            var logHelper = LogHelper.Resolve();
            applicationLifetime.ApplicationStopping.Register(OnShutdownNLog, logHelper);
        }
        
        private static void OnShutdownNLog(object state)
        {
            var logHelper = state as ILogHelper;
            try
            {
                logHelper?.Info(">>>> OnShutdownNLog");
                NLog.LogManager.Shutdown();
            }
            catch (Exception e)
            {
                logHelper?.Error(e, ">>>> OnShutdownNLog Error");
            }
        }
    }
}
