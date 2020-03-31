using Kanbersky.SearchPanther.Business.DTO.Response;
using Kanbersky.SearchPanther.Core.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kanbersky.SearchPanther.Business.Abstract
{
    public interface IElasticSearchService
    {
        Task<IResult> CheckCluster();

        Task<IDataResult<BaseEllaResponse>> CreateIndexAsync();

        Task<IResult> CreateMultiDocumentAsync<T>(List<T> entities) where T : class;

        Task<IResult> CreateDocumentAsync<T>(T entity) where T : class;

        Task<IResult> DeleteIndexAsync();

        Task<IDataResult<T>> GetByIdAsync<T>(int id) where T : class;

        Task<IResult> DeleteDocumentByIdAsync<T>(int id) where T : class;

        Task<IDataResult<T>> UpdateAllDocument<T>(int id,T entity) where T : class;

        Task<IResult> UpSertDocument<T>(int id, T entity) where T : class;

        Task<IDataResult<T>> InsertNewDocument<T>(T entity) where T : class;

        Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchAllQuery();

        Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchSingleColumn(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchTermSingleColumn(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchTermMultiColumn(string productTerm, string categoryTerm);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchPhrasePrefixSingleColumn(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchMatchPhraseSingleColumn(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchMultiMatchMultiColumn(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SortQuery();

        Task<IDataResult<List<ProductCategoryResponse>>> SearchNestedBoolQuery(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchWithFuziness(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchWitPrefix(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> SearchWitWildcard(string term);

        Task<IDataResult<List<ProductCategoryResponse>>> AutoCompleteEasy(string term);

        Task<IDataResult<BaseEllaResponse>> CreateIndexWithAnalyzersAsync();

        Task<IDataResult<List<ProductCategoryResponse>>> AutoCompleteWithAnalyzers(string term);
    }

}
