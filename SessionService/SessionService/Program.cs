
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SessionService.Interfaces;
using System.Security.Claims;
using System.Text;

namespace SessionService
{
    public class Program
    {
        public static string rabbitUsername;
        public static string rabbitPassword;
        public static void Main(string[] args)
        {

#if DEBUG
            DevelopEnvLoader.Load("develop.env");
#endif

            var builder = WebApplication.CreateBuilder(args);


            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddSingleton<ISessionDatabase, SessionDatabase>();

            rabbitUsername = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER");
            rabbitPassword = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS");

            string rabbitHost = Environment.GetEnvironmentVariable("SERVICE_ALIAS_RABBIT");
            int rabbitPort = Int32.Parse(Environment.GetEnvironmentVariable("SERVICE_PORT_RABBIT"));
            string exchangeName = Environment.GetEnvironmentVariable("EXCHANGE_SESSIONEVENTBROADCAST");
            builder.Services.AddSingleton<Publisher>(new Publisher(rabbitHost, rabbitPort, exchangeName));

            string queueName = Environment.GetEnvironmentVariable("QUEUE_STATECHANGE");
            builder.Services.AddSingleton<Consumer>(new Consumer(rabbitHost, rabbitPort, queueName));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token in the text input below."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY"))),
                    RoleClaimType = ClaimTypes.Role // This ensures that role claims are used for authorization
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
