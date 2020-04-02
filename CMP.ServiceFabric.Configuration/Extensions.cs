using System;
using System.Fabric;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.ServiceFabric.AspNetCore.Configuration;

namespace CMP.ServiceFabric.Configuration
{
    public static class Extensions
    {
        public static IConfigurationRefresher Refresher { get; private set; }

        public static IConfigurationRoot AddCmpConfiguration(this IConfigurationBuilder configBuilder, string version, bool isInCluster = true)
        {
            configBuilder.AddEnvironmentVariables();
            configBuilder.AddServiceFabricSettings(isInCluster);
            configBuilder.AddAppSettings();
            configBuilder.AddVaultSettings();
            configBuilder.AddAzureAppSettings(version);

            return configBuilder.Build();
        }

        private static void AddServiceFabricSettings(this IConfigurationBuilder configBuilder, bool isInCluster)
        {
            if (isInCluster)
            {
                var context = FabricRuntime.GetActivationContext();
                configBuilder.AddServiceFabricConfiguration(context, options
                    => options.IncludePackageName = false);
            }
        }

        private static void AddAppSettings(this IConfigurationBuilder configBuilder)
        {
            var config = configBuilder.Build();
            var environment = config["ASPNETCORE_ENVIRONMENT"];

            configBuilder.AddYamlFile("appsettings.yml", true, true);

            if (!string.IsNullOrEmpty(environment))
            {
                configBuilder.AddYamlFile($"appsettings.{environment}.yml", true, true);
            }
        }

        private static void AddVaultSettings(this IConfigurationBuilder configBuilder)
        {
            var config = configBuilder.Build();
            var vault = config["KeyVault:Name"];
            var clientId = config["KeyVault:ClientId"];
            var clientSecret = config["KeyVault:ClientSecret"];

            if (HasVaultSettings(vault, clientId, clientSecret))
            {
                configBuilder.AddAzureKeyVault(
                    $"https://{vault}.vault.azure.net/",
                    clientId,
                    clientSecret);
            }
        }

        private static bool HasVaultSettings(string vault, string clientId, string clientSecret)
            => vault != null && clientId != null && clientSecret != null;

        public static void AddAzureAppSettings(this IConfigurationBuilder configBuilder, string version)
        {
            var config = configBuilder.Build();
            var appConfigConnection = config["ConnectionStrings:CmpAzureAppConfig"];

            if (!string.IsNullOrEmpty(appConfigConnection))
            {
                configBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(appConfigConnection)
                        // Setup dynamic refresh
                        .ConfigureRefresh(refresh =>
                        {
                            // Update all settings when the value of given key changes
                            refresh.Register(version, true)
                                .SetCacheExpiration(TimeSpan.FromSeconds(1));
                        });
                    // Enable on demand dynamic configuration in .Net Core console app
                    Refresher = options.GetRefresher();
                });
            }
        }
    }
}
