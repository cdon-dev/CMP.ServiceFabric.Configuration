using System.Fabric;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.AspNetCore.Configuration;

namespace CMP.ServiceFabric.Configuration
{
    public static class Extensions
    {
        public static IConfigurationRoot AddCmpConfiguration(this IConfigurationBuilder configBuilder, bool isInCluster)
        {
            configBuilder.AddEnvironmentVariables();
            configBuilder.AddServiceFabricSettings(isInCluster);
            configBuilder.AddAppSettings();
            configBuilder.AddVaultSettings();

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
    }
}
