using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using WebixKanban;
using WebixKanban.Data;
using WebixKanban.Data.Model;

namespace WebApp1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            IoC.Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(IoC.Configuration.GetConnectionString("DefaultConnection"))
            );

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 2;
                options.Password.RequireUppercase = false;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            });

            // JWT 
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = IoC.Configuration["Jwt:myIssuer"],
                        ValidAudience = IoC.Configuration["Jwt:myAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IoC.Configuration["Jwt:SecretKey"]))
                    };
                });

            // Cookies
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            CheckDatabaseCreatedAndMigrated(serviceProvider);
            SeedDatabase(serviceProvider);

            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void SeedDatabase(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            if (SeedRoles(roleManager))
                SeedUsers(userManager);
        }

        private bool SeedUsers(UserManager<ApplicationUser> userManager)
        {
            try
            {
                if (userManager.FindByNameAsync("Admin").Result == null)
                {
                    ApplicationUser admin = new ApplicationUser
                    {
                        UserName = "Admin",
                        Email = "admini@speedmail.pl"
                    };

                    var result = userManager.CreateAsync(admin, "password").Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(admin, "Admin").Wait();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SeedRoles(RoleManager<ApplicationRole> roleManager)
        {
            try
            {
                if (!roleManager.RoleExistsAsync("Admin").Result)
                {
                    ApplicationRole role = new ApplicationRole();
                    role.Name = "Admin";
                    IdentityResult roleResult = roleManager.CreateAsync(role).Result;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void CheckDatabaseCreatedAndMigrated(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetService<ApplicationDbContext>();

            context.Database.EnsureCreated();
            context.Database.Migrate();

            SeedDatabase(serviceProvider);
        }
    }
}
