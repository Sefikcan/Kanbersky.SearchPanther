using Kanbersky.SearchPanther.Core.Setting;
using Microsoft.Extensions.Options;
using Nest;
using System;

namespace Kanbersky.SearchPanther.Core.Helpers.ElasticSearch
{
    public class ElasticClientProvider
    {
        public ElasticClient Client { get; set; }

        //https://www.elastic.co/guide/en/elasticsearch/client/net-api/7.x/configuration-options.html referance
        //https://www.elastic.co/guide/en/elasticsearch/client/net-api/7.x/modifying-default-connection.html elasticsearch birim testi için kullanabilirsin
        //https://www.elastic.co/guide/en/elasticsearch/client/net-api/7.x/mapping.html elasticsearch mapping işlemleri
        public ElasticClientProvider(IOptions<ElasticSearchSettings> settings)
        {
            ConnectionSettings connectionSettings = new ConnectionSettings(new Uri(settings.Value.ServerUrl));
            connectionSettings
                .EnableDebugMode();
              //.RequestTimeout(2) //sunucunun bir isteği ne kadar sürede iptal etmesi gerektiğini belirlemek için tüm isteklere uygulanacak varsayılan istek zaman aşımını belirtiyoruz
                //.DisablePing() yavaş response dönen isteklerin fail olmasını istersek ekleyebilirsin
                //.SniffOnStartup(false) //solution ilk ayağa kalktığında elastic'e istek atılıp atılmayacağını belirleyebiliriz.
                //.SniffOnConnectionFault(false);

            Client = new ElasticClient(connectionSettings);
        }

    }
}
