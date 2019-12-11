using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using mypos_api.Database;
using mypos_api.repo;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AutoMapper;

namespace mypos_api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // เป็นระดับ service
        public IConfiguration Configuration { get; } // Configuration ไปดึงไฟล์ appsetting.json

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(); // AddNewtonsoftJson ให้อ่าน json แบบไม่มี limit

            // ประกาศ Declare Database Service for DI (dependency injection)
            services.AddDbContext<DatabaseContext>(options =>
               options.UseSqlServer(Configuration.GetConnectionString("ConnectionSQLServer")));  // ต่อ SQL SERVER

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ToDo API",

                });

                c.SwaggerDoc("v2", new OpenApiInfo
                {
                    Version = "v2",
                    Title = "POS API",
                    Description = "A simple example ASP.NET Core Web API",
                    TermsOfService = new Uri("http://codemobiles.com"),
                    Contact = new OpenApiContact
                    {
                        Name = "iBlur Blur",
                        Email = "codemobiles@gmail.com",
                        Url = new Uri("http://codemobiles.com"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under MIT",
                        Url = new Uri("http://codemobiles.com"),
                    },
                });
                // ปุ่ม Authorize ใช้ได้เฉพาะ JWT
                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                c.AddSecurityDefinition("Bearer", securitySchema);

                var securityRequirement = new OpenApiSecurityRequirement();
                securityRequirement.Add(securitySchema, new[] { "Bearer" });
                c.AddSecurityRequirement(securityRequirement);

                // Customize
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                // Uses full schema names to avoid v1/v2/v3 schema collisions
                // see: https://github.com/domaindrivendev/Swashbuckle/issues/442
                c.CustomSchemaIds(x => x.FullName);
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Validate JWT จาก appsetiings
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = Configuration["Jwt:Audience"],
                    ValidateLifetime = true, // ExpireDay
                    ClockSkew = TimeSpan.Zero, // disable delay when token is expire
                    ValidateIssuerSigningKey = true, // Key
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                };
            });


            services.AddCors(options =>
            {
                // AllowSpecificOrigins ยอมรับทุกๆ Header, Method
                options.AddPolicy("AllowSpecificOrigins", builder =>
                {
                    builder.WithOrigins("http://example.com", "http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                    //.WithMethods("GET", "POST", "HEAD"); // การกำหนดสิทธิ์ว่าให้สามารถทำอะไรได้บ้าง
                });

                // เปิดสิทธิ์ให้ทุกคนใช้ request ข้อมูลได้ ไม่ security
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });

                /*
                    The browser can skip the preflight request
                    if the following conditions are true:
                    - The request method is GET, HEAD, or POST.
                    - The Content-Type header
                       - application/x-www-form-urlencoded
                       - multipart/form-data
                       - text/plain
                */
            });

            // Declare IProductRepo Service for DI ยกตัว IProductRepo ให้เป็นระดับ Service
            services.AddScoped<IProductRepo, ProductRepo>();
            services.AddScoped<IAuthRepo, AuthRepo>(); // ต้องมาเรียกใช้ service ทุกครั้ง
            services.AddAutoMapper(typeof(Startup));
            // access hosting
            services.AddHttpContextAccessor(); // ประยุกต์ใช้กับอัพโหลดรูปภาพ
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Middle ware 
            // Mode Development จะโชว์ bug
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection(); // http => https

            app.UseRouting();

            app.UseCors("AllowAll"); // ไปเรียกใช้ AllowAll

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            // http://localhost:<port>/swagger/<version-doc>/swagger.json
            // mark: <version-doc> ref. c.SwaggerDoc("v1", ....)
            app.UseSwagger(); // คำสั่ง gen json

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            // http://localhost:<port>/swagger
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
                c.DisplayOperationId();
                c.DisplayRequestDuration();
                // To serve the Swagger UI at the app's root 
                //c.RoutePrefix = string.Empty;
            });

            app.UseAuthentication(); // ยืนยันตัวตน (ก่อนตรวจสอบสิทธิ์)

            app.UseAuthorization(); // ตรวจสอบสิทธิ์

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
