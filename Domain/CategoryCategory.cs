using Nop.Core;

namespace Nop.Plugin.Misc.MultipleParents.Domain
{
    /// <summary>
    /// Represents a category category mapping
    /// </summary>
    public partial class CategoryCategory : BaseEntity
    {
        /// <summary>
        /// Gets or sets the parent category identifier
        /// </summary>
        public virtual int? ParentCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the child category identifier
        /// </summary>
        public virtual int ChildCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public virtual int DisplayOrder { get; set; }
    }
}
