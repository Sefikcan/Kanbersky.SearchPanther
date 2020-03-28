using Kanbersky.SearchPanther.Core.Setting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System;

namespace Kanbersky.SearchPanther.Core.Helpers.Logging
{
    public class LoggerHelpers
    {
        private readonly ElasticSearchSettings _elasticsearchSettings;

        public LoggerHelpers(ElasticSearchSettings elasticsearchSettings)
        {
            _elasticsearchSettings = elasticsearchSettings;
        }

        public ILogger Register(string applicationName, LogEventLevel logEventLevel)
        {
            var logConf = new LoggerConfiguration();
            logConf.MinimumLevel.Override("Microsoft", logEventLevel);
            logConf.MinimumLevel.Override("System", logEventLevel);
            logConf.MinimumLevel.Verbose();
            logConf.Enrich.FromLogContext();
            logConf.Enrich.WithProperty("Application", applicationName);
            logConf.WriteTo.Console();

            if (_elasticsearchSettings != null && !string.IsNullOrEmpty(_elasticsearchSettings.ServerUrl))
            {
                logConf.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(_elasticsearchSettings.ServerUrl)));
            }

            return logConf.CreateLogger();
        }
    }
}
