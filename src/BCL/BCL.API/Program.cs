using BCL.Core;
using BCL.Discord;
using BCL.Domain;
using BCL.Persistence.Sqlite;

using Hangfire;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

DomainConfig.Setup(builder.Configuration);
CoreConfig.Setup(builder.Configuration);
PersistenceSqliteConfig.Setup(builder.Configuration);
DiscordConfig.Setup(builder.Configuration);

builder.Services.AddPersistenceSqlite();
builder.Services.AddCoreServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddHangfire();
builder.Services.AddDiscordEngine();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseHangfireDashboard();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
//app.UseEndpoints(endpoints => { endpoints.MapHangfireDashboard(); });
app.MapControllers();

app.MigrateSqliteDatabase();
app.UseSqliteDatabaseSeed();
await app.UseDiscord();

app.Run();
