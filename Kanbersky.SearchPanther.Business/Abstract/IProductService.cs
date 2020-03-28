using Kanbersky.SearchPanther.Business.DTO.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kanbersky.SearchPanther.Business.Abstract
{
    public interface IProductService
    {
        Task<ProductResponse> GetProductById(int id);

        IEnumerable<ProductCategoryResponse> ProductCategoryResponses();
    }
}
