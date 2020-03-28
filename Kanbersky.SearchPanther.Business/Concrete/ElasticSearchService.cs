using Kanbersky.SearchPanther.Business.Abstract;
using Kanbersky.SearchPanther.Business.DTO.Response;
using Kanbersky.SearchPanther.Core.Constants;
using Kanbersky.SearchPanther.Core.Helpers.ElasticSearch;
using Kanbersky.SearchPanther.Core.Helpers.ElasticSearch.Abstract;
using Kanbersky.SearchPanther.Core.Results;
using Kanbersky.SearchPanther.Entities.Abstract;
using Microsoft.AspNetCore.Http;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanbersky.SearchPanther.Business.Concrete
{
    public class ElasticSearchService : IElasticSearchService
    {
        #region fields

        private readonly ElasticClient _elasticClient;

        #endregion

        #region ctor

        public ElasticSearchService(ElasticClientProvider clientProvider)
        {
            _elasticClient = clientProvider.Client;
        }

        #endregion

        #region analyzer bilgileri 

        // Analyzer indeksleme ve arama sürecinde metinlerin üzerinde değişiklik yapar. 5 tip analyzer vardor
        // SimpleAnalyzer: LetterTokenizer ve LowerCaseFilter'ı içerir.
        // StopAnalyzer: LetterTokenizer,LowerCaseFilter ve StopFilter'i içerir
        // WhitespaceAnalyzer: WhitespaceTokenizer'ı kullanır ve text'i boşluklarına ayırır.
        // KeywordAnalyzer: Gelen Token(keyword)'i tamamını parçalara ayırmadan kullanmaya yarar.Ürün ismi arama,posta kodu arama gibi..
        // StandardAnalyzer: StandardTokenizer,StandardFilter,LowerCaseFilter ve StopFilter'ı içerir.

        // ***Dokümanı indexlerken kullanmış olduğunuz analyzer tipini, doğru scoring alabilmek ve sonuçları elde edebilmek adına search işleminde de kullanmanız gerekmektedir.

        // Her sorguya Profile eklersen hangi sorgunun yavaş olduğunu görebilirsin.

        #endregion
        
        #region crud methods

        /// <summary>
        /// Elastic'de yeni index oluşturmayı sağlar
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public async Task<IDataResult<BaseEllaResponse>> CreateIndexAsync()
        {
            //İlgili IndexName'ine göre kayıt kontrolü yapılır
            var indexIsExists = await _elasticClient.Indices.ExistsAsync(ElasticSearchConstants.DefaultIndexName);
            if (indexIsExists.Exists)
            {
                //Alias ismine göre index'leri listeler
                var alias2IndexList = await _elasticClient.GetIndicesPointingToAliasAsync(ElasticSearchConstants.DefaultIndexName);
                if (alias2IndexList.Count() > 0)
                {
                    foreach (var item in alias2IndexList)
                    {
                        //Eğer kayıt varsa index silinir.
                        await _elasticClient.Indices.DeleteAsync(item);
                    }
                }
            }

            //Yeni index için yeni indexName verilir.
            var newIndexName = ElasticSearchConstants.DefaultIndexName + +DateTime.Now.Ticks;

            CreateIndexResponse createIndex = await _elasticClient.Indices.CreateAsync(newIndexName,
                ss => ss.Index(newIndexName)
                .Settings(
                    o => o.NumberOfShards(4).NumberOfReplicas(2) //İlgili veriyi 4 shard'a dağıttık ve her sharddın 2 replikası olacak şekilde index ayarlamamızı yaptık
                    .Setting("max_result_window", int.MaxValue) // veriniz eğer gerçekten de büyük ise karşınıza çıkacak bir Elasticsearch’te de bazı sınırlar mevcut. Bu sınırlamayı en kısa şöyle ifade edebiliriz                           => from + size <= index.max_result_window. Biz burada default 10000 değerini int max değerine set ettik
                    .Analysis(a => a
                        .TokenFilters(tkf => tkf
                        .AsciiFolding("my_ascii_folding", af => af.PreserveOriginal(true))) // Token Filterlar arasına Ascii Folding Token Filter eklersek tokenlar içerisindeki Türkçe karakterler Ascii benzerlerine dönüşecektir. İki türde de arama yaparsanız döküman eşleşecektir.
                        .Analyzers(aa => aa
                        .Custom("turkish_analyzer", ca => ca
                        .Filters("lowercase", "my_ascii_folding")
                        .Tokenizer("standard") //standardanalyzer kullandığımızı belirtiyoruz.
                        )))
                    )
                .Map(r => r
                .AutoMap()));

            //Acknowledged değer true ise index'imizi başarıyla oluşturduk demektir.
            if (createIndex.Acknowledged)
            {
                //Index'imiz başarıyla oluştuktan sonra alias'ımızı ekliyoruz
                await _elasticClient.Indices.BulkAliasAsync(ba => ba.Add(a => a.Index(newIndexName).Alias(ElasticSearchConstants.DefaultIndexName)));
                return new SuccessDataResult<BaseEllaResponse>(new BaseEllaResponse { IndexName = newIndexName }, StatusCodes.Status200OK);
            }

            //olası bir hata durumunda belirlediğimiz exception formatında hata fırlatıyoruz.
            throw new ElasticSearchException($"Create Index {ElasticSearchConstants.DefaultIndexName} failed : :" + createIndex.ServerError.Error.Reason);
        }

        /// <summary>
        /// İlgili IndexName'ine göre index silme işlemini sağlar
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public async Task<IResult> DeleteIndexAsync()
        {
            var response = false;
            //Alias ismine göre index'leri listeler
            var alias2IndexList = await _elasticClient.GetIndicesPointingToAliasAsync(ElasticSearchConstants.DefaultIndexName);
            if (alias2IndexList.Count() > 0)
            {
                foreach (var item in alias2IndexList)
                {
                    //Eğer kayıt varsa index silinir.
                    var deleteIndexResponse = await _elasticClient.Indices.DeleteAsync(item);
                    response = deleteIndexResponse.Acknowledged;
                    if (!response)
                    {
                        //Silme Başarısızsa belirlediğimiz formatta hata fırlatırız
                        throw new ElasticSearchException($"Delete index {item} failed :{deleteIndexResponse.ServerError.Error.Reason}");
                    }
                }
            }

            if (response)
                return new SuccessResult(StatusCodes.Status200OK);

            return new ErrorResult(StatusCodes.Status400BadRequest);
        }

        /// <summary>
        /// İlgili IndexName'e göre çoklu döküman ekleme işlemini sağlar
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public async Task<IResult> CreateMultiDocumentAsync<T>(List<T> entities) where T : class
        {
            //Her ihtimale karşılık index sorgulaması yapılır.
            var indexIsExists = await _elasticClient.Indices.ExistsAsync(ElasticSearchConstants.DefaultIndexName);
            if (indexIsExists.Exists)
            {
                //İlgili indexe çoklu döküman ekleme işlemini yapıyoruz
                var indexCreated = await _elasticClient.IndexManyAsync(entities, index: ElasticSearchConstants.DefaultIndexName);
                if (indexCreated.IsValid)
                {
                    return new SuccessResult(StatusCodes.Status200OK);
                }
            }

            return new ErrorResult(StatusCodes.Status400BadRequest);
        }

        /// <summary>
        /// İlgili indexName'e göre tekli döküman ekleme işlemini sağlar
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public async Task<IResult> CreateDocumentAsync<T>(T entity) where T : class
        {
            //Her ihtimale karşılık index sorgulaması yapılır.
            var indexIsExists = await _elasticClient.Indices.ExistsAsync(ElasticSearchConstants.DefaultIndexName);
            if (indexIsExists.Exists)
            {
                //İlgili dökümana index ekleme işlemini yapıyoruz
                var documentCreated = await _elasticClient.IndexAsync(entity, s => s.Index(ElasticSearchConstants.DefaultIndexName));
                if (documentCreated.IsValid)
                {
                    return new SuccessResult(StatusCodes.Status200OK);
                }
            }

            return new ErrorResult(StatusCodes.Status400BadRequest);
        }

        /// <summary>
        /// İlgili indexName'in id parametresine göre dökümanını getirir.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IDataResult<T>> GetByIdAsync<T>(int id) where T : class
        {
            var response = await _elasticClient.GetAsync(new DocumentPath<T>(id), s => s.Index(ElasticSearchConstants.DefaultIndexName));
            if (response != null)
            {
                return new SuccessDataResult<T>(response.Source, StatusCodes.Status200OK);
            }

            return new ErrorDataResult<T>("Not Found", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// İlgili indexName'in id parametresine göre dökümanını siler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IResult> DeleteDocumentByIdAsync<T>(int id) where T : class
        {
            var response = await _elasticClient.DeleteAsync(new DocumentPath<T>(id).Index(ElasticSearchConstants.DefaultIndexName));
            if (response.IsValid)
            {
                return new SuccessResult(StatusCodes.Status200OK);
            }

            return new SuccessResult("Not Found", StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// İlgili indexName'in id parametresine göre gönderilen entity modelini elastic'de günceller
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<IDataResult<T>> UpdateAllDocument<T>(int id, T entity) where T : class
        {
            var response = await _elasticClient.UpdateAsync(new DocumentPath<T>(id), u => u.Doc(entity).Index(ElasticSearchConstants.DefaultIndexName));
            if (response.IsValid)
            {
                return new SuccessDataResult<T>(entity, StatusCodes.Status200OK);
            }

            return new ErrorDataResult<T>("Not Found", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// İlgili id ve entity bilgisine göre dökümanı günceller
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<IResult> UpSertDocument<T>(int id,T entity) where T : class
        {
            var response = await _elasticClient.UpdateAsync(DocumentPath<T>
                .Id(id),
                u => u
                    .Index(ElasticSearchConstants.DefaultIndexName)
                    .DocAsUpsert(true)
                    .Doc(entity));
            if (response.IsValid)
            {
                return new SuccessResult(StatusCodes.Status200OK);
            }

            return new ErrorResult(StatusCodes.Status400BadRequest);
        }

        /// <summary>
        /// İlgili indexname'e yeni row ekleme işlemini yapar
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<IDataResult<T>> InsertNewDocument<T>(T entity) where T : class
        {
            var response = await _elasticClient.BulkAsync(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .Create<T>(s => s.Document(entity)));

            if (response.IsValid)
            {
                return new SuccessDataResult<T>(entity, StatusCodes.Status200OK);
            }

            return new ErrorDataResult<T>("Not Found", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// Cluster'ın durumunu kontrol etmek için kullanılır
        /// </summary>
        /// <returns></returns>
        public async Task<IResult> CheckCluster()
        {
            var response = await _elasticClient.Cluster.HealthAsync();
            if (response.IsValid)
            {
                return new SuccessResult(StatusCodes.Status200OK);
            }

            return new ErrorResult(StatusCodes.Status500InternalServerError);
        }

        #endregion

        #region search methods

        #region search hakkında bilgiler

        // 3 tip sorgu tipi vardır.
        // 1.Structured search => Sorgunun yanıtı true yada false'dur.Sorguya göre döküman eşleşir yada eşleşmez.
        // 2.UnStructured search => Full text search işlemleri buraya girer.
        // 3.Combining Queries => Bool ile ayrı ayrı sorguları birleştirme işlemidir.

        #endregion

        /// <summary>
        /// Elastic index'imizde tüm dataları matchall sorgusu ile geri döndürebiliriz.
        /// </summary>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchAllQuery()
        {
            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .Query(q => q
            .MatchAll()));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("Kayıt Bulunamadı!", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// Match tam eşleme ister.Örnek vermek gerekirse Data Şefik Can olsun.Şefik ve Can query'leri için Şefik Can datası dönecektir.Ancak Şef yazdığınızda data dönmeyecektir.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchSingleColumn(string term)
        {
            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .From(0)
            .Size(10)
            .Query(q => q
            .Match(m => m
            .Field(f => f.ProductName)
            .Query(term))));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!",null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// Term - Terms bire bir eşleme gerektiren alanların aranmasında kullanılır.Match'e göre farkı term case-sensitive olarak çalışıyor.Ayrıca burdaki çıktı analiz edilmez
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchTermSingleColumn(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
            }

            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .From(0)
            .Size(10)
            .Query(q => q
            .Term(t => t
            .Field(p=>p.ProductName)
            .Value(term.ToLower()))));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// Terms birden çok term query'sindeki veriyi birleştirip geriye döndürür.
        /// </summary>
        /// <param name="productTerm"></param>
        /// <param name="categoryTerm"></param>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchTermMultiColumn(string productTerm, string categoryTerm)
        {
            if (string.IsNullOrEmpty(productTerm) || string.IsNullOrEmpty(categoryTerm))
            {
                return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
            }

            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .From(0)
            .Size(10)
            .Query(q => q
            .Terms(tt => tt
            .Field(p => p.ProductName)
            .Terms(productTerm.ToLower())
            .Field(c => c.CategoryName)
            .Terms(categoryTerm.ToLower()))));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// MatchPhrasePrefix'de aranan kelime veya cümle geçmesi yeterlidir, bire bir eşleme istemez
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchPhrasePrefixSingleColumn(string term)
        {
            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .From(0)
            .Size(10)
            .Query(q => q
            .MatchPhrasePrefix(m => m
            .Field(f => f.ProductName)
            .Query(term))));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// MatchPhrase'de aranan kelime veya cümlenin bire bir eşleşmesi gerekmektedir.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchPhraseSingleColumn(string term)
        {
            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .From(0)
            .Size(10)
            .Query(q => q
            .MatchPhrase(m => m
            .Field(f => f.ProductName)
            .Query(term))));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
        }

        /// <summary>
        /// Multimatch'in çalışma mantığı match ile aynı şekilde.Fark olarak tek query içinde birden fazla kolonda verdiğimiz önceliğe göre arama yapıyoruz.Yürütme şekli Type parametresine bağlıdır.
        ///   BestFields   : Herhangi bir alanla eşleşen dokümanları bulur, ancak en iyi alandan _score kullanır.(varsayılan)
        ///   MostFields   : Herhangi bir alanla eşleşen dökümanları bulur ve her alandan _score’u birleştirir.
        ///   CrossFields  : Alanlara aynı analizörle büyük bir alanmış gibi davranır. Herhangi bir alandaki her kelimeyi arar.
        ///   Phrase       : Her alanda bir match_phrase sorgusu çalıştırır ve en iyi alandan _score kullanır.
        ///   PhrasePrefix : Her alanda bir match_phrase_prefix sorgusu çalıştırır ve en iyi alandan _score kullanır.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public async Task<IDataResult<List<ProductCategoryResponse>>> SearchMultiMatchMultiColumn(string term)
        {
            var response = await _elasticClient.SearchAsync<ProductCategoryResponse>(s => s
            .Index(ElasticSearchConstants.DefaultIndexName)
            .From(0)
            .Size(10)
            .Query(q => q
            .MultiMatch(mm => mm
            .Fields(f => f.Field(ff => ff.ProductName, 2.0) //Fields içindeki boost önceliklendirme değeridir.Burada productname'in önceliği categoryName'in önceliğinden fazla olacak şekilde işlem yapılmıştır
            .Field(ff => ff.CategoryName, 1.0))
            .Query(term)
            .Type(TextQueryType.BestFields)
            .Operator(Operator.Or) // AND ile de dene
            .MinimumShouldMatch(3)
            )));

            if (response.IsValid && response.Documents.Any())
            {
                return new SuccessDataResult<List<ProductCategoryResponse>>(response.Documents.ToList(), StatusCodes.Status200OK);
            }

            return new ErrorDataResult<List<ProductCategoryResponse>>("İlgili search term'e göre data bulunamadı!", null, StatusCodes.Status404NotFound);
        }

        #endregion

        #region nested bool query 

        /* bool tipinde sorgular
        * must=>Cümle (sorgu) eşleşen belgelerde görünmelidir ve skora katkıda bulunacaktır.
        * filter=> Yan tümce (sorgu) eşleşen belgelerde görünmelidir. Ancak zorunluluktan farklı olarak, sorgunun puanı dikkate alınmaz.
        * should=> Yan tümce (sorgu) eşleşen belgede görünmelidir. Zorunlu veya filtre yan tümcesi olmayan bir boole sorgusunda, bir veya daha fazla yan tümce, bir belgeyle eşleşmelidir. Eşleşmesi gereken minimum koşul cümlesi sayısı minimum_should_match parametresi kullanılarak ayarlanabilir.
        * must_not=> Yan tümce (sorgu) eşleşen belgelerde görünmemelidir.*/

        #endregion
    }

}
