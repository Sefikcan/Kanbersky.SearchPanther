using Kanbersky.SearchPanther.Core.Entity;
using Kanbersky.SearchPanther.Entities.Abstract;

namespace Kanbersky.SearchPanther.Entities.Concrete
{
    public class Product : BaseEntity, IEntity
    {
        public string ProductName { get; set; }

        public int SupplierId { get; set; }

        public int CategoryId { get; set; }

        public decimal UnitPrice { get; set; }

        public string QuantityPerUnit { get; set; }

        public short UnitsInStock { get; set; }

        public short UnitsOnOrder { get; set; }

        public short ReorderLevel { get; set; }

        public virtual Category Category { get; set; }
    }
}
