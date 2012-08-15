using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.MultipleParents.Models
{
    public class CategoryModel
    {
        public Nop.Admin.Models.Catalog.CategoryModel BaseViewModel { get; set; }

        public int CategoryId { get; set; }

        public int NumberOfAvailableCategories { get; set; }

        public class CategoryCategoryModel : BaseNopEntityModel
        {
            [NopResourceDisplayName("Plugin.Misc.MultipleParents.Fields.Category")]
            [UIHint("CategoryCategory")]
            public string Category { get; set; }

            public int? ParentCategoryId { get; set; }

            public int ChildCategoryId { get; set; }

            [NopResourceDisplayName("Plugin.Misc.MultipleParents.Fields.DisplayOrder")]
            //we don't name it DisplayOrder because Telerik has a small bug 
            //"if we have one more editor with the same name on a page, it doesn't allow editing"
            //in our case it's category.DisplayOrder and products.DisplayOrder
            public int DisplayOrder1 { get; set; }
        }
    }
}
