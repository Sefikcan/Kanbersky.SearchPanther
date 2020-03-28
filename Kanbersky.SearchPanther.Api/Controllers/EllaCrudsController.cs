using System.Linq;
using System.Threading.Tasks;
using Kanbersky.SearchPanther.Business.Abstract;
using Kanbersky.SearchPanther.Business.DTO.Request;
using Kanbersky.SearchPanther.Business.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kanbersky.SearchPanther.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EllaCrudsController : ControllerBase
    {
        #region fields

        private readonly IProductService _productService;
        private readonly IElasticSearchService _elasticSearchService;

        #endregion

        #region ctor

        public EllaCrudsController(IProductService productService,
            IElasticSearchService elasticSearchService)
        {
            _productService = productService;
            _elasticSearchService = elasticSearchService;
        }

        #endregion

        #region crud methods

        /// <summary>
        /// İlgili Indexe Toplu Döküman Eklenmesini sağlar
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("multi-entity-create-index")]
        [ProducesResponseType(typeof(ProductCategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateListEntityIndex()
        {
            var entity = _productService.ProductCategoryResponses();
            var searchResponse = entity.Select(x => new ProductCategoryResponse
            {
                Id = x.ProductId,
                ProductId = x.ProductId,
                CategoryId = x.CategoryId,
                ProductName = x.ProductName,
                CategoryName = x.CategoryName,
                QuantityPerUnit = x.QuantityPerUnit
            });

            var result = await _elasticSearchService.CreateIndexAsync();
            if (result.Success)
            {
                var documentResult = await _elasticSearchService.CreateMultiDocumentAsync(searchResponse.ToList());
                return StatusCode(documentResult.StatusCode, documentResult);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili index'e tekli döküman eklenmesini sağlar.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("one-entity-create-index")]
        [ProducesResponseType(typeof(ProductCategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOneEntityIndex()
        {
            var entity = _productService.ProductCategoryResponses();
            var searchResponse = entity.Select(x => new ProductCategoryResponse
            {
                Id = x.ProductId,
                ProductId = x.ProductId,
                CategoryId = x.CategoryId,
                ProductName = x.ProductName,
                CategoryName = x.CategoryName,
                QuantityPerUnit = x.QuantityPerUnit
            });

            var result = await _elasticSearchService.CreateIndexAsync();
            if (result.Success)
            {
                var documentResult = await _elasticSearchService.CreateDocumentAsync(searchResponse.FirstOrDefault());
                return StatusCode(documentResult.StatusCode, documentResult);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili index'i siler
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteIndex()
        {
            var result = await _elasticSearchService.DeleteIndexAsync();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Id'ye göre elastic'deki ilgili datayı geri döndürür.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _elasticSearchService.GetByIdAsync<ProductCategoryResponse>(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili id'ye göre index'deki dökümanı siler
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteDocumentById(int id)
        {
            var result = await _elasticSearchService.DeleteDocumentByIdAsync<ProductCategoryResponse>(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili id ve entity bilgisine göre dökümanı günceller.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productCategoryResponse"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAllDocument(int id, [FromBody] UpdateProductCategoryRequest updateProductCategoryRequest)
        {
            var product = await _productService.GetProductById(id);
            if (product == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var result = await _elasticSearchService.UpdateAllDocument(id, updateProductCategoryRequest);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili indexname'e entity'yi set eder
        /// </summary>
        /// <param name="createProductCategoryRequest"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDocument([FromBody] CreateProductCategoryRequest createProductCategoryRequest)
        {
            createProductCategoryRequest.Id = createProductCategoryRequest.ProductId;
            var result = await _elasticSearchService.InsertNewDocument(createProductCategoryRequest);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cluster'ın durumunu kontrol etmek için kullanılır
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("check-cluster")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CheckCluster()
        {
            var result = await _elasticSearchService.CheckCluster();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili id ve entity bilgisine göre dökümanı günceller
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productCategoryResponse"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("upsert/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertDocument(int id, [FromBody] UpdateProductCategoryRequest updateProductCategoryRequest)
        {
            var product = await _productService.GetProductById(id);
            if (product == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var result = await _elasticSearchService.UpSertDocument(id, updateProductCategoryRequest);
            return StatusCode(result.StatusCode, result);
        }

        #endregion

        #region search methods

        /// <summary>
        /// Elastic'deki tüm datayı geri döndürür
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("match_all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchMatchAll()
        {
            var result = await _elasticSearchService.SearchMatchAllQuery();
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili term'e göre arama yapar
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search-match/{term}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchWithMatch(string term)
        {
            var result = await _elasticSearchService.SearchMatchSingleColumn(term);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili Term'e göre arama yapar
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search-term/{term}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchWithTerm(string term)
        {
            var result = await _elasticSearchService.SearchTermSingleColumn(term);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili Term'e göre arama yapar
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search-term-multi/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchWithTermMulti([FromQuery]string productTerm, [FromQuery]string categoryTerm)
        {
            var result = await _elasticSearchService.SearchTermMultiColumn(productTerm, categoryTerm);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili Term'e göre arama yapar
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search-matchphraseprefix/{term}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchWithMatchPhrasePrefix(string term)
        {
            var result = await _elasticSearchService.SearchMatchPhrasePrefixSingleColumn(term);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili Term'e göre arama yapar
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search-matchphrase/{term}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchWithMatchPhrase(string term)
        {
            var result = await _elasticSearchService.SearchMatchPhraseSingleColumn(term);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// İlgili Term'e göre arama yapar
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search-multimatch/{term}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchWithMultiMatch(string term)
        {
            var result = await _elasticSearchService.SearchMultiMatchMultiColumn(term);
            return StatusCode(result.StatusCode, result);
        }

        #endregion
    }
}