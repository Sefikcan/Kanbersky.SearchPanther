using Nest;

namespace Kanbersky.SearchPanther.Core.Helpers.ElasticSearch.Abstract
{
    public class BaseElasticEntity<T>
    {
        public virtual T Id { get; set; }
        public virtual CompletionField Suggest { get; set; }
        public virtual string SearchingArea { get; set; }
        public virtual double? Score { get; set; }
    }
}
