
using HealthService.Classes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace HealthService
{
    public class Program
    {
        public const string heartbeatTimespan = "HEARTBEAT_TIMESPAN_SECONDS";
        public const string clientsCheckedPerFrame = "CLIENTS_CHECKED_PER_FRAME";
        public static string endSessionAddress;
        public static TimeSpan HeartbeatDuration { get; private set; }
        public static void Main(string[] args)
        {
#if DEBUG
            DevelopEnvLoader.Load("develop.env");
#endif
            endSessionAddress = Environment.GetEnvironmentVariable("SERVICE_ALIAS_SESSION") + "api/endsession";
            SystemSetup(args);
            RestSetup(args);
        }
        private static void SystemSetup(string[] args)
        {
            HeartbeatDuration = TimeSpan.FromSeconds(double.Parse(Environment.GetEnvironmentVariable(heartbeatTimespan)));
            Console.WriteLine(HeartbeatDuration);
        }
        private static void RestSetup(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.


            builder.Services.AddControllers();
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

            //Make the TrackerHandler here and start it and all.
            //Controllers run their constructor over and over every time an endpoint is called
            //Thus, we need to use the Singleton feature here and set it all up in here
            int count = int.Parse(Environment.GetEnvironmentVariable(Program.clientsCheckedPerFrame));
            TrackerHandler trackerHandler = new TrackerHandler(count);
            trackerHandler.StartHeartbeatChecking();
            builder.Services.AddSingleton(trackerHandler);

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
