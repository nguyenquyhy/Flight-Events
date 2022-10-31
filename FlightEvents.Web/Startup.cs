using FlightEvents.Data;
using FlightEvents.Data.AzureStorage;
using FlightEvents.Web.GraphQL;
using FlightEvents.Web.Hubs;
using FlightEvents.Web.Identity;
using FlightEvents.Web.Logics;
using HotChocolate;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
#if DEBUG
            IdentityModelEventSource.ShowPII = true;
#endif
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<FeaturesOptions>().Bind(Configuration.GetSection("Features")).ValidateDataAnnotations();
            services.AddOptions<EventOptions>().Bind(Configuration.GetSection("Events")).ValidateDataAnnotations();
            services.AddOptions<DiscordOptions>().Bind(Configuration.GetSection("Discord")).ValidateDataAnnotations();
            services.AddOptions<AzureBlobOptions>().Bind(Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();
            services.AddOptions<AzureTableDiscordOptions>().Bind(Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();
            services.AddOptions<AzureTableLeaderboardOptions>().Bind(Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();
            services.AddOptions<AzureTableUserOptions>().Bind(Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();
            services.AddOptions<BroadcastOptions>().Bind(Configuration.GetSection("Broadcast")).ValidateDataAnnotations();

            services.AddSingleton<RandomStringGenerator>();
            services.AddSingleton<IFlightEventStorage, JsonFileFlightEventStorage>();
            services.AddSingleton<IFlightPlanFileStorage, AzureBlobFlightPlanFileStorage>();
            services.AddSingleton<IAirportStorage, XmlFileAirportStorage>();
            services.AddSingleton<IDiscordConnectionStorage, AzureTableDiscordConnectionStorage>();
            services.AddSingleton<ILeaderboardStorage, AzureTableLeaderboardStorage>();
            services.AddSingleton<IATCFlightPlanStorage, InMemoryATCFlightPlanStorage>();

            services.AddGraphQL()
                    .AddQueryType<QueryType>()
                    .AddMutationType<MutationType>()
                    .AddType<FlightEventQueryType>()
                    .AddType<FlightEventChecklistItemTypeEnumType>()
                    .AddAuthorization();

            services.AddControllersWithViews();
            services.AddRazorPages();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(config =>
                {
                    config.ForwardChallenge = "Microsoft";
                })
                .AddOpenIdConnect("Microsoft", config =>
                {
                    Configuration.Bind("Authentication:Microsoft", config);

                    //config.Scope.Add("openid");
                    //config.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    config.TokenValidationParameters.IssuerValidator = new IssuerValidator((issuer, token, parameters) =>
                    {
                        // Accepts any issuer of the form "https://login.microsoftonline.com/{tenantid}/v2.0",
                        // where tenantid is the tid from the token.

                        if (token is JwtSecurityToken jwt)
                        {
                            if (jwt.Payload.TryGetValue("tid", out var value) &&
                                value is string tokenTenantId)
                            {
                                var issuers = (parameters.ValidIssuers ?? Enumerable.Empty<string>())
                                    .Append(parameters.ValidIssuer)
                                    .Where(i => !string.IsNullOrEmpty(i));

                                if (issuers.Any(i => i.Replace("{tenantid}", tokenTenantId) == issuer))
                                    return issuer;
                            }
                        }

                        // Recreate the exception that is thrown by default
                        // when issuer validation fails
                        var validIssuer = parameters.ValidIssuer ?? "null";
                        var validIssuers = parameters.ValidIssuers == null
                            ? "null"
                            : !parameters.ValidIssuers.Any()
                                ? "empty"
                                : string.Join(", ", parameters.ValidIssuers);
                        string errorMessage = FormattableString.Invariant(
                            $"IDX10205: Issuer validation failed. Issuer: '{issuer}'. Did not match: validationParameters.ValidIssuer: '{validIssuer}' or validationParameters.ValidIssuers: '{validIssuers}'.");

                        throw new SecurityTokenInvalidIssuerException(errorMessage)
                        {
                            InvalidIssuer = issuer
                        };
                    });

                    config.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            if (context.Request.Path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString("/api")))
                            {
                                // Prevent default redirect behavior for /api endpoints
                                context.Response.StatusCode = 401;
                                context.HandleResponse();
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

            services.AddSingleton<IUserStorage, AzureTableUserStorage>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("EventManager", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StopwatchManager", policy => policy.RequireRole("Admin", "Mod"));
            });

            var builder = services.AddSignalR();
            if (!string.IsNullOrWhiteSpace(Configuration["Azure:SignalR:ConnectionString"]))
            {
                builder.AddAzureSignalR();
            }
            builder.AddMessagePackProtocol();

            services.AddHttpClient<DiscordLogic>();

            services.AddHostedService<StatusBroadcastService>();
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

            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGraphQL();
                endpoints.MapGraphQLVoyager();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();

                endpoints.MapHub<FlightEventHub>("/FlightEventHub");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
