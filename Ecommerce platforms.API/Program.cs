using Ecommerce_platforms.Core.IRepository;
using Ecommerce_platforms.Repository.Auth;
using Ecommerce_platforms.Repository.Data;
using Ecommerce_platforms.Repository.Data.Identity;
using Ecommerce_platforms.Repository.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Text;
using System.Text.Json.Serialization;

namespace Ecommerce_platforms.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);

            var app = builder.Build();

            ConfigurePipeline(app);

            await SeedDatabase(app);

            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ? Configuring Identity with Strong Password Policy
            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                // Password Strength Policy
                options.Password.RequireDigit = true;             // Must contain a number
                options.Password.RequireLowercase = true;         // Must contain a lowercase letter
                options.Password.RequireUppercase = true;         // Must contain an uppercase letter
                options.Password.RequireNonAlphanumeric = true;   // Must contain a special character
                options.Password.RequiredLength = 8;              // Minimum length
                options.Password.RequiredUniqueChars = 1;         // Minimum number of unique characters

                // Lockout Policy (Optional)
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);  // Lockout time
                options.Lockout.MaxFailedAccessAttempts = 5;  // Lockout after 5 failed attempts
                options.Lockout.AllowedForNewUsers = true;

                // User Policy
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // ? JWT Authentication Configuration
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                        ValidAudience = builder.Configuration["JWT:ValidAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])),
                        ClockSkew = TimeSpan.FromMinutes(5),
                        RoleClaimType = "role"
                    };
                });

            // ? Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOrSubAdmin", policy => policy.RequireClaim("role", "Admin", "SubAdmin"));
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("AdminOrSubAdminOrUser", policy => policy.RequireClaim("role", "Admin", "SubAdmin", "User"));
            });

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // ? CORS Configuration
            builder.Services.AddCors(options =>
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()));

            // ? Swagger Configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce Platforms API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by your token."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ? Dependency Injection for Services & Repositories
            builder.Services.AddScoped<IAuth, Auth>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICart, CartRepository>();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<IProduct, ProductRepository>();
            builder.Services.AddScoped<IBrand, BrandRepository>();
            builder.Services.AddScoped<IDeliveryMethod, DeliveryMethodRepository>();
            builder.Services.AddScoped<IOrder, OrderRepository>();

            // ? Stripe Configuration
            StripeConfiguration.ApiKey = builder.Configuration["StripeSettings:SecretKey"];
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowAll");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
        }

        private static async Task SeedDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            await IdentityUserDataSeeding.SeedUserAsync(userManager, roleManager);
        }
    }
}
