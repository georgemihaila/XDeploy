using System.Collections.Generic;
using System.Linq;

namespace XDeploy.Core.Extensions
{
    /// <summary>
    /// Helper methods for lists.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Splits a list into chunks.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
