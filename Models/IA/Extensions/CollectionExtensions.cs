using System.Collections.Generic;
using System.Linq;

namespace LifeHub.Models.IA.Extensions
{
    public static class CollectionExtensions
    {
        public static decimal Variance(this IEnumerable<decimal> values)
        {
            if (!values.Any()) return 0;
            
            var average = values.Average();
            var sumOfSquares = values.Sum(x => (x - average) * (x - average));
            return sumOfSquares / (values.Count() - 1);
        }
    }
}