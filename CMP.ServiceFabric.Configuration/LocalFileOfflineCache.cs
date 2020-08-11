using System.IO;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace CMP.ServiceFabric.Configuration
{
    public class LocalFileOfflineCache : IOfflineCache
    {
        private readonly string _cacheFilePath;

        public LocalFileOfflineCache(string filePath)
        {
            _cacheFilePath = Path.Combine(filePath, "offlineCache.json");
        }

        public void Export(AzureAppConfigurationOptions options, string data)
        {
            File.WriteAllText(_cacheFilePath, data);
        }

        public string Import(AzureAppConfigurationOptions options)
        {
            return File.ReadAllText(_cacheFilePath);
        }
    }
}
