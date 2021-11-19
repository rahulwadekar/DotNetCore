using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using EmployeeManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement
{
    public class Startup
    {
        private IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();
            services.AddDbContextPool<AppDbContext>(options => options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));
            //services.AddMvc(options => options.EnableEndpointRouting = false).AddXmlSerializerFormatters();

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).AddXmlSerializerFormatters();

            //services.AddSingleton<IEmployeeRepository, MockEmployeeRepository>();
            //services.AddScoped<IEmployeeRepository, MockEmployeeRepository>();
            //services.AddTransient<IEmployeeRepository, MockEmployeeRepository>();
            //OR
            //services.AddControllers(options => options.EnableEndpointRouting = false);

            //services.AddMvcCore(options => options.EnableEndpointRouting = false); called internally by AddMVC()

            //services.Configure<IdentityOptions>(options => //not required as same is done by AddIdentity
            //{
            //    options.Password.RequiredLength = 3;
            //    options.Password.RequiredUniqueChars = 0;
            //});

            //to change default access denied route
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("DeleteRolePolicy",
            //        policy => policy.RequireClaim("Delete Role"));

            //    options.AddPolicy("EditRolePolicy",
            //        policy => policy.RequireClaim("Edit Role", "true"));

            //    options.AddPolicy("AdminRolePolicy",
            //        policy => policy.RequireClaim("Admin"));
            //});

            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("EditRolePolicy", policy => policy.RequireAssertion(context =>
            //        context.User.IsInRole("Admin") &&
            //        context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
            //        context.User.IsInRole("Super Admin")
            //    ));
            //});


            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("EditRolePolicy", policy =>
            //        policy.RequireAssertion(context => AuthorizeAccess(context)));
            //});

            services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = "XXX";
                options.ClientSecret = "XXX";
            });

            //Custom authorization policy with handlers
            services.AddAuthorization(options =>
            {
                options.AddPolicy("EditRolePolicy", policy =>
                    policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));

                //options.InvokeHandlersAfterFailure = false; // to stop executing other handlers from executing
            });

            services.AddSingleton<IAuthorizationHandler,
                CanEditOnlyOtherAdminRolesAndClaimsHandler>();

            // Register the second handler
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();




            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("DeleteRolePolicy",
            //        policy => policy.RequireClaim("Delete Role")
            //                        .RequireClaim("Create Role")

            //        );
            //}); //add multiple claims

            // Changes token lifespan of all token types
            services.Configure<DataProtectionTokenProviderOptions>(o =>
                    o.TokenLifespan = TimeSpan.FromHours(5));

            // Changes token lifespan of just the Email Confirmation Token type
            services.Configure<CustomEmailConfirmationTokenProviderOptions>(o =>
                    o.TokenLifespan = TimeSpan.FromDays(3));

            services.AddSingleton<DataProtectionPurposeStrings>();


            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 3;
                    options.Password.RequiredUniqueChars = 0;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                }).AddEntityFrameworkStores<AppDbContext>()
                  .AddDefaultTokenProviders()
                  .AddTokenProvider<CustomEmailConfirmationTokenProvider<ApplicationUser>>("CustomEmailConfirmation");


        }


        private bool AuthorizeAccess(AuthorizationHandlerContext context)
        {
            return context.User.IsInRole("Admin") &&
                    context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
                    context.User.IsInRole("Super Admin");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                //DeveloperExceptionPageOptions developerExceptionPageOptions = new DeveloperExceptionPageOptions();
                //developerExceptionPageOptions.SourceCodeLineCount = 10;
                app.UseDeveloperExceptionPage(/*developerExceptionPageOptions*/);
            }
            else
            {
                app.UseExceptionHandler("/Error");
                //app.UseStatusCodePages(); ///just like default 404 so not used in prod
                //app.UseStatusCodePagesWithRedirects("/Error/{0}");
                app.UseStatusCodePagesWithReExecute("/Error/{0}"); //reexecute the pipeline with /error/404
            }

            //else if(env.IsStaging() || env.IsProduction() || env.IsEnvironment("UAT"))
            //{
            //    app.UseExceptionHandler("/errorpage.html");
            //}

            //DefaultFilesOptions options = new DefaultFilesOptions();
            //options.DefaultFileNames.Clear();
            //options.DefaultFileNames.Add("foo.html");

            //app.UseDefaultFiles(options);            


            //FileServerOptions fileServerOptions = new FileServerOptions();
            //fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
            //fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("foo.html");

            //app.UseFileServer(fileServerOptions);            
            //app.UseFileServer(); // combines functionality of UseDefaultFiles and UseStaticFiles


            app.UseStaticFiles(); // Middleware to ender static files.

            app.UseAuthentication();
            //app.UseMvcWithDefaultRoute(); // when matchine rout found it becomes terminal middleware

            //CONVENTIONAL ROUTING
            //add routes explicitly
            //set default routes =
            //set optional routes with ?
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=index}/{id?}");
                //routes.MapRoute("default", "companyname/{controller=Home}/{action=index}/{id?}"); // this automatically works with tag helpers
            }); // when matchine rout found it becomes terminal middleware

            //app.UseMvc();

            //app.UseRouting();

            //app.Use(async (context, next) =>
            //{   
            //    //throw new Exception();

            //    logger.LogInformation("MW1- Incoming Request");
            //    await context.Response.WriteAsync("Hello from 1 middleware");
            //    await next();
            //    logger.LogInformation("MW1- Outgoing Request");

            //});

            //app.Use(async (context, next) =>
            //{
            //    logger.LogInformation("MW2- Incoming Request");
            //    await context.Response.WriteAsync("Hello from 1 middleware");
            //    await next();
            //    logger.LogInformation("MW2- Outgoing Request");

            //});

            //app.Run(async (context) =>
            //{
            //    //logger.LogInformation("MW3- Incoming Request");
            //    //await context.Response.WriteAsync("Hello from 2 middleware");
            //    //await context.Response.WriteAsync("Request handeleted by MW3 and produced output");
            //    //await context.Response.WriteAsync(env.EnvironmentName);
            //    //logger.LogInformation("MW3- Incoming Request");
            //    await context.Response.WriteAsync("Hello World!");

            //});

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapGet("/", async context =>
            //    {
            //        //await context.Response.WriteAsync("Hello World!");
            //        //await context.Response.WriteAsync(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            //        //await context.Response.WriteAsync(_config["MyKey"]);
            //        await context.Response.WriteAsync("Hello from 3 middleware");
            //    });
            //});

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapGet("/temp", async context =>
            //    {
            //        //await context.Response.WriteAsync("Hello World!");
            //        //await context.Response.WriteAsync(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            //        //await context.Response.WriteAsync(_config["MyKey"]);
            //        await context.Response.WriteAsync("Hello from 4 middleware");
            //    });
            //});
        }
    }
}
