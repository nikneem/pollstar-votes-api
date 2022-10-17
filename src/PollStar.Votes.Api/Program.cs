using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PollStar.Core.Configuration;
using PollStar.Core.HealthChecks;
using PollStar.Votes;

const string defaultCorsPolicyName = "default_cors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureConfiguration>(
    builder.Configuration.GetSection(AzureConfiguration.SectionName));

builder.Services.AddPollStarCore(builder.Configuration);
builder.Services.AddPollStarVotes();

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
    ep.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckExtensions.WriteResponse
    });
    ep.MapControllers();
});

app.Run();
