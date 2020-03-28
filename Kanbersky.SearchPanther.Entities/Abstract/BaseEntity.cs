using System.ComponentModel.DataAnnotations;

namespace Kanbersky.SearchPanther.Entities.Abstract
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
