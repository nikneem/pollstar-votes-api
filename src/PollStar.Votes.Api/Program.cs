using PollStar.Core.Configuration;
using PollStar.Votes;

const string defaultCorsPolicyName = "default_cors";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//var environmentVariables = Environment.GetEnvironmentVariables();
//var config = new AzureConfiguration();
//builder.Configuration.GetSection(AzureConfiguration.SectionName).Bind(config);
//if (!environmentVariables.Contains(EnvironmentVariableName.AzureStorageAccountName))
//{
//    Environment.SetEnvironmentVariable(EnvironmentVariableName.AzureStorageAccountName, config.StorageAccount);
//}
//if (!environmentVariables.Contains(EnvironmentVariableName.AzureStorageAccountKey))
//{
//    Environment.SetEnvironmentVariable(EnvironmentVariableName.AzureStorageAccountKey, config.StorageKey);
//}

builder.Services.Configure<AzureConfiguration>(
    builder.Configuration.GetSection(AzureConfiguration.SectionName));

builder.Services.AddPollStarVotes();

builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: defaultCorsPolicyName,
        bldr =>
        {
            bldr.WithOrigins("http://localhost:4200",
                    "https://pollstar-dev.hexmaster.nl",
                    "https://pollstar-test.hexmaster.nl",
                    "https://pollstar.hexmaster.nl")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(defaultCorsPolicyName);

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(ep =>
{
    ep.MapHealthChecks("/health");
    ep.MapControllers();
});

app.Run();
