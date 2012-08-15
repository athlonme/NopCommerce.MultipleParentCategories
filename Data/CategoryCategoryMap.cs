using Nop.Plugin.Misc.MultipleParents.Domain;
using System.Data.Entity.ModelConfiguration;

namespace Nop.Plugin.Misc.MultipleParents.Data
{
    public partial class CategoryCategoryMap : EntityTypeConfiguration<CategoryCategory>
    {
        public const string TableName = "Category_Category_Mapping";

        public CategoryCategoryMap()
        {
            this.ToTable(TableName);
            this.HasKey(cc => cc.Id);
        }
    }
}
