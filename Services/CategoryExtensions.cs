using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.MultipleParents.Services
{
    public static class CategoryExtensions
    {
        /// <summary>
        /// Sort categories for tree representation
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="parentId">Parent category identifier</param>
        /// <param name="ignoreCategoriesWithoutExistingParent">A value indicating whether categories without parent category in provided category list (source) should be ignored</param>
        /// <returns>Sorted categories</returns>
        public static IList<Category> SortCategoriesForTree(this IList<Category> source, int? parentId, bool ignoreCategoriesWithoutExistingParent = false)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var result = new List<Category>();

            //foreach (var cat in source.ToList().FindAll(c => c.ParentCategories.Any(pc => (parentId.HasValue ? pc.ParentCategoryId == parentId : pc.ParentCategoryId == null))))
            //{
            //    result.Add(cat);
            //    result.AddRange(SortCategoriesForTree(source, cat.Id, true));
            //}
            if (!ignoreCategoriesWithoutExistingParent && result.Count != source.Count)
            {
                //find categories without parent in provided category source and insert them into result
                foreach (var cat in source)
                    if (result.Where(x => x.Id == cat.Id).FirstOrDefault() == null)
                        result.Add(cat);
            }
            return result;
        }
    }
}
