using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MassTransitDemo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static AppConfig AppConfig { get; set; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddRazorPages();

            services.AddHealthChecks();

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
                options.Predicate = (check) => check.Tags.Contains("ready");
            });

            services.Configure<AppConfig>(Configuration.GetSection("AppConfig"));

            services.AddMassTransit(cfg =>
            {
                cfg.SetKebabCaseEndpointNameFormatter();

                cfg.AddBus(context => ConfigureBus(context));
            });

            services.AddMassTransitHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();

                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("ready"),
                });

                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions());
            });
        }

        static IBusControl ConfigureBus(IBusRegistrationContext context)
        {
            AppConfig = context.GetRequiredService<IOptions<AppConfig>>().Value;

            X509Certificate2? x509Certificate2 = null;

            if (AppConfig.SSLActive)
            {
                x509Certificate2 = GetCertificate(StoreName.My, StoreLocation.LocalMachine, AppConfig.SSLThumbprint);
            }

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(AppConfig.Host, AppConfig.VirtualHost, h =>
                {
                    h.Username(AppConfig.Username);
                    h.Password(AppConfig.Password);

                    if (AppConfig.SSLActive)
                    {
                        h.UseSsl(ssl =>
                        {
                            ssl.ServerName = Dns.GetHostName();
                            ssl.AllowPolicyErrors(SslPolicyErrors.RemoteCertificateNameMismatch);
                            ssl.Certificate = x509Certificate2;
                            ssl.Protocol = SslProtocols.Tls12;

                            ssl.CertificateSelectionCallback
                            = (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers) =>
                            {
                                var serverCertificate = localCertificates.OfType<X509Certificate2>()
                                        .FirstOrDefault(cert => cert.Thumbprint.ToLower() == AppConfig.SSLThumbprint.ToLower());

                                return serverCertificate ?? throw new Exception("Wrong certificate");
                            };
                        });
                    }
                });

                cfg.ConfigureEndpoints(context);
            });

        }

        private static X509Certificate2? GetCertificate(StoreName storeName, StoreLocation storeLocation, string sSLThumbprint)
        {
            X509Certificate2? x509Certificate = null;

            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certificatesInStore = store.Certificates;

                x509Certificate = certificatesInStore.OfType<X509Certificate2>()
                    .FirstOrDefault(cert =>
                    {
                        return cert != null && string.Equals(
                               cert.Thumbprint?.ToLower(),
                               sSLThumbprint?.ToLower(),
                               StringComparison.OrdinalIgnoreCase
                           );
                    });
            }
            finally
            {
                store.Close();
            }

            return x509Certificate;
        }
    }
}
