using HullCellReport.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HullCellReport.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JWTRegen.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using HullCellReport.Middlewares;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.Loader;

namespace HullCellReport
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
            services.AddControllersWithViews();

            #region JWT
            services.AddScoped<JWTRegen.Interfaces.IJwtTokenService, JWTRegen.Services.JwtTokenService>();
            services.AddScoped<JWTRegen.Interfaces.IClaimsHelper, JWTRegen.Services.ClaimsHelper>();
            var jwtSettingsSection = Configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettingsSection);

            var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
            var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;  // Enable in production (false for local testing)
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["hullcellreport_jwt"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            #endregion

            #region repository
            services.AddScoped(typeof(DapperService));
            services.AddScoped(typeof(EmployeeRepository));
            #endregion

            #region DinkToPdf
            var context = new CustomAssemblyLoadContext();
            var architectureFolder = (IntPtr.Size == 8) ? "64 bit" : "32 bit";
            var wkHtmlToPdfPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), $"wkhtmltopdf");
            
            // Try to load from wkhtmltopdf folder first
            var libPath = System.IO.Path.Combine(wkHtmlToPdfPath, "libwkhtmltox.dll");
            if (!System.IO.File.Exists(libPath))
            {
                // Fallback to default path
                libPath = System.IO.Path.Combine(wkHtmlToPdfPath, architectureFolder, "libwkhtmltox.dll");
            }
            
            context.LoadUnmanagedLibrary(libPath);
            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Support for subfolder deployment (e.g., /HullCellReport)
            var pathBase = Configuration["PathBase"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.Use(async (context, next) =>
            {
                try
                {
                    await next.Invoke();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Token has expired"))
                    {
                        if (context.Request.Cookies.ContainsKey("hullcellreport_jwt"))
                        {
                            context.Response.Cookies.Delete("hullcellreport_jwt");
                        }
                        var pathBase = Configuration["PathBase"] ?? "";
                        context.Response.Redirect($"{pathBase}/Auth/vLogin");
                    }
                    else
                    {
                        throw;
                    }
                }
            });

            app.UseMiddleware<JWTRegen.Middleware.TokenVersionMiddleware>();
            app.UseMiddleware<RedirectUnauthorizedMiddleware>();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Auth}/{action=vLogin}/{id?}");
            });
        }
    }
}


    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            return LoadUnmanagedDll(absolutePath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return LoadUnmanagedDllFromPath(unmanagedDllName);
        }

        protected override System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyName)
        {
            throw new NotImplementedException();
        }
    }
