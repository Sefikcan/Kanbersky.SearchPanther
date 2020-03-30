using Kanbersky.SearchPanther.Business.Abstract;
using Kanbersky.SearchPanther.Business.DTO.Response;
using Kanbersky.SearchPanther.DAL.Concrete.EntityFramework.GenericRepository;
using Kanbersky.SearchPanther.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kanbersky.SearchPanther.Business.Concrete
{
    public class ProductService : IProductService
    {
        #region fields

        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<Category> _categoryRepository;

        #endregion

        #region ctor

        public ProductService(IGenericRepository<Product> productRepository,
            IGenericRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        #endregion

        #region methods

        public IEnumerable<ProductCategoryResponse> ProductCategoryResponses()
        {
            var product = _productRepository.GetQueryable();
            var category = _categoryRepository.GetQueryable();

            var productCategory = from p in product
                                  join c in category on p.CategoryId equals c.Id
                                  select new
                                  {
                                      p.Id,
                                      CategoryId = c.Id,
                                      p.ProductName,
                                      c.CategoryName,
                                      p.QuantityPerUnit,
                                      p.UnitPrice
                                  };

            var ProductCategoryResponse = productCategory.Select(x => new ProductCategoryResponse
            {
                ProductId = x.Id,
                CategoryId = x.CategoryId,
                ProductName = x.ProductName,
                CategoryName = x.CategoryName,
                QuantityPerUnit = x.QuantityPerUnit,
                UnitPrice = x.UnitPrice
            });
            return ProductCategoryResponse.ToList();
        }

        public async Task<ProductResponse> GetProductById(int id)
        {
            var productResponse = new ProductResponse();
            var product = await _productRepository.Get(x => x.Id == id);
            if (product != null)
            {
                productResponse.Id = product.Id;
                productResponse.ProductName = product.ProductName;
                productResponse.QuantityPerUnit = product.QuantityPerUnit;
                productResponse.ReorderLevel = product.ReorderLevel;
                productResponse.UnitPrice = product.UnitPrice;
                product.UnitsInStock = product.UnitsInStock;
                product.UnitsOnOrder = product.UnitsOnOrder;
            }

            return productResponse;
        }

        #endregion
    }
}
