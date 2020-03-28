namespace Kanbersky.SearchPanther.Business.DTO.Request
{
    public class UpdateProductCategoryRequest
    {
        public int ProductId { get; set; }

        public int CategoryId { get; set; }

        public string ProductName { get; set; }

        public string CategoryName { get; set; }

        public string QuantityPerUnit { get; set; }
    }
}
