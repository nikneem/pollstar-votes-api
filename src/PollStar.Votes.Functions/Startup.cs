using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using PollStar.Votes.Functions;
using System;
using HexMaster.RedisCache;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(Startup))]
namespace PollStar.Votes.Functions
{
    public class Startup : FunctionsStartup
    {

        private IConfigurationRoot _configuration;
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var azureCredential = new DefaultAzureCredential();

            try
            {
                var azureAppConfigurationEndpoint = Environment.GetEnvironmentVariable("AzureAppConfiguration") ??"";
                builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(new Uri(azureAppConfigurationEndpoint), azureCredential)
                        .ConfigureKeyVault(kv => kv.SetCredential(azureCredential))
                        .UseFeatureFlags();
                });
                _configuration = builder.ConfigurationBuilder.Build();
            }
            catch (Exception ex)
            {
                throw new Exception("Configuration failed", ex);
            }

            base.ConfigureAppConfiguration(builder);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHexMasterCache(_configuration);
        }
    }
}
