using Microsoft.Extensions.Configuration;
using System.IO;

namespace Kanbersky.SearchPanther.Core.Helpers.Logging
{
    public static class BaseHelpers
    {
        public static IConfigurationRoot GetConfigurationRoot(string[] args)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json", false, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
        }
    }
}
