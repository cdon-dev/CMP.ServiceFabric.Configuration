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

        public static IConfigurationRoot AddCmpConfiguration(this IConfigurationBuilder configBuilder, bool isInCluster = true, string filePathOfflineCache = "")
        {
            configBuilder.AddEnvironmentVariables();
            configBuilder.AddServiceFabricSettings(isInCluster);
            configBuilder.AddAppSettings();
            configBuilder.AddVaultSettings();
            configBuilder.AddAzureAppSettings(filePathOfflineCache);

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

        public static void AddAzureAppSettings(this IConfigurationBuilder configBuilder, string filePathOfflineCache)
        {
            var config = configBuilder.Build();
            var connectionString = config["ConnectionStrings:CmpAzureAppConfig"];
            var environment = config["ASPNETCORE_ENVIRONMENT"] ?? LabelFilter.Null;

            if (!string.IsNullOrEmpty(connectionString))
            {
                configBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(connectionString)
                        // Setup dynamic refresh
                        .ConfigureRefresh(refresh =>
                        {
                            // Update all settings when the value of given key changes
                            refresh.Register("RefreshVersion", true)
                                .SetCacheExpiration(TimeSpan.FromMinutes(5));
                        })
                        // Load configuration values with no label
                        .Select(KeyFilter.Any)
                        // Override with any configuration values specific to current hosting env
                        .Select(KeyFilter.Any, environment);

                    if (!string.IsNullOrEmpty(filePathOfflineCache))
                    {
                        // Use the custom IOfflineCache implementation
                        options.SetOfflineCache(new LocalFileOfflineCache(filePathOfflineCache));
                    }

                    // Enable on demand dynamic configuration in .Net Core console app
                    Refresher = options.GetRefresher();
                });
            }
        }
    }
}
