using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Security;
using Nop.Core.Events;
using Nop.Core.Plugins;
using Nop.Plugin.Misc.MultipleParents.Domain;
using Nop.Services.Catalog;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.MultipleParents.Services
{
    /// <summary>
    /// Category service override which utilizes category-category mappings.
    /// </summary>
    public partial class CategoryServiceExt : CategoryService, ICategoryService
    {
        #region Copied constants

        private const string CATEGORIES_BY_ID_KEY = "Nop.category.id-{0}";
        private const string CATEGORIES_BY_PARENT_CATEGORY_ID_KEY = "Nop.category.byparent-{0}-{1}";
        private const string CATEGORIES_BY_PARENT_CATEGORY_ID_TREE_KEY = "Nop.category.byparent-tree-{0}-{1}";
        private const string CATEGORIES_PATTERN_KEY = "Nop.category.";

        #endregion

        #region New constants

        private const string CATEGORYCATEGORIES_ALLBYCATEGORYID_KEY = "Nop.categorycategory.allbycategoryid-{0}-{1}";
        private const string CATEGORYCATEGORIES_ALLBYPARENTCATEGORYID_KEY = "Nop.categorycategory.allbyparentcategoryid-{0}-{1}";
        private const string CATEGORYCATEGORIES_BY_ID_KEY = "Nop.categorycategory.id-{0}";
        private const string CATEGORYCATEGORIES_PATTERN_KEY = "Nop.categorycategory.";
        private const string CATEGORY_ALLPARENTCHILDCATEGORIES_KEY = "Nop.category.allparentchildcategories-{0}-{1}";

        #endregion

        #region Fields

        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<CategoryCategory> _categoryCategoryRepository;

        private readonly IPluginFinder _pluginFinder;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="categoryRepository">Category repository</param>
        /// <param name="productCategoryRepository">ProductCategory repository</param>
        /// <param name="productRepository">Product repository</param>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="categoryCategoryRepository">CategoryCategory repository</param>
        /// <param name="categoryExRepository">CategoryEx repository</param>
        public CategoryServiceExt(ICacheManager cacheManager,
            IRepository<Category> categoryRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<Product> productRepository,
            IRepository<AclRecord> aclRepository,
            IWorkContext workContext,
            IEventPublisher eventPublisher,
            IRepository<CategoryCategory> categoryCategoryRepository,
            IPluginFinder pluginFinder)
            : base(cacheManager, categoryRepository, productCategoryRepository, productRepository, aclRepository, workContext, eventPublisher)
        {
            this._eventPublisher = eventPublisher;
            this._cacheManager = cacheManager;
            this._categoryCategoryRepository = categoryCategoryRepository;
            this._categoryRepository = categoryRepository;

            this._pluginFinder = pluginFinder;
        }

        #endregion

        #region Properties

        public bool Installed
        {
            get
            {
                return (_pluginFinder != null && _pluginFinder.GetPluginDescriptorBySystemName("Misc.MultipleParents", false).Installed);
            }
        }

        #endregion

        #region Method overrides

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="categoryName">Category name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Categories</returns>
        public override IPagedList<Category> GetAllCategories(string categoryName = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            if (!Installed)
                return base.GetAllCategories(categoryName, pageIndex, pageSize, showHidden);

            return GetAllCategories(categoryName, pageIndex, pageSize, showHidden, false);
        }

        /// <summary>
        /// Gets all categories filtered by parent category identifier
        /// </summary>
        /// <param name="parentCategoryId">Parent category identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Category collection</returns>
        IList<Category> ICategoryService.GetAllCategoriesByParentCategoryId(int parentCategoryId,
            bool showHidden = false)
        {
            if (!Installed)
                return base.GetAllCategoriesByParentCategoryId(parentCategoryId, showHidden);

            return GetAllCategoriesByParentCategoryId(parentCategoryId == 0 ? null : new int?(parentCategoryId), showHidden);
        }

        /// <summary>
        /// Gets all categories displayed on the home page
        /// </summary>
        /// <returns>Category collection</returns>
        public override IList<Category> GetAllCategoriesDisplayedOnHomePage()
        {
            if (!Installed)
                return base.GetAllCategoriesDisplayedOnHomePage();

            return GetAllCategoriesDisplayedOnHomePageEx();
        }

        /// <summary>
        /// Gets a category
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <returns>Category</returns>
        public override Category GetCategoryById(int categoryId)
        {
            if (categoryId == 0)
                return null;

            string key = string.Format(CATEGORIES_BY_ID_KEY, categoryId);
            return _cacheManager.Get(key, () =>
            {
                var query = (from c in _categoryRepository.Table
                                join cc in _categoryCategoryRepository.Table on c.Id equals cc.ChildCategoryId
                                where c.Id == categoryId
                                select new { Category = c, ParentCategoryId = cc.ParentCategoryId }).FirstOrDefault();

                var category = query.Category;
                category.ParentCategoryId = query.ParentCategoryId.GetValueOrDefault(0);

                return category;
            });
        }

        #endregion

        #region New method implementations

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="showTree">A value indicating whether to show the full category tree</param>
        /// <returns>Categories</returns>
        public virtual IList<Category> GetAllCategories(bool showHidden = false, bool showTree = false)
        {
            return GetAllCategories(null, showHidden, showTree);
        }

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="categoryName">Category name</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="showTree">A value indicating whether to show the full category tree</param>
        /// <returns>Categories</returns>
        public virtual IList<Category> GetAllCategories(string categoryName, bool showHidden = false, bool showTree = false)
        {
            var query = from c in _categoryRepository.Table
                        join cc in _categoryCategoryRepository.Table on c.Id equals cc.ChildCategoryId into ParentCategories
                        orderby (ParentCategories.Any() ? ParentCategories.FirstOrDefault().Id : 0),
                                (ParentCategories.Any() ? ParentCategories.FirstOrDefault().DisplayOrder : 0)
                        select c;
            if (!showHidden)
                query = query.Where(c => c.Published);
            if (!String.IsNullOrWhiteSpace(categoryName))
                query = query.Where(c => c.Name.Contains(categoryName));
            query = query.Where(c => !c.Deleted);
            var unsortedCategories = query.ToList();

            if (showTree)
            {
                //sort categories
                var sortedCategories = unsortedCategories.SortCategoriesForTree(null);
                return sortedCategories.ToList();
            }
            else
            {
                return unsortedCategories.ToList();
            }
        }

        /// <summary>
        /// Gets all categories
        /// </summary>
        /// <param name="categoryName">Category name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="showTree">A value indicating whether to show the full category tree</param>
        /// <returns>Categories</returns>
        public virtual IPagedList<Category> GetAllCategories(string categoryName,
            int pageIndex, int pageSize, bool showHidden = false, bool showTree = false)
        {
            var categories = GetAllCategories(categoryName, showHidden, showTree);
            //filter
            return new PagedList<Category>(categories, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets all categories filtered by parent category identifier
        /// </summary>
        /// <param name="parentCategoryId">Parent category identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="includeCategoryCategoryId">A value indicating whether to include the 
        /// category-category identifier in the category name (for the tree view)</param>
        /// <returns>Category collection</returns>
        public virtual IList<Category> GetAllCategoriesByParentCategoryId(int? parentCategoryId,
            bool showHidden = false)
        {
            return GetAllTreeCategoriesByParentCategoryId(parentCategoryId, showHidden).Select(ct => ct.Category).ToList();
        }

        /// <summary>
        /// Gets all categories filtered by parent category identifier
        /// </summary>
        /// <param name="parentCategoryId">Parent category identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="includeCategoryCategoryId">A value indicating whether to include the 
        /// category-category identifier in the category name (for the tree view)</param>
        /// <returns>Category collection</returns>
        public virtual IList<CategoryTuple> GetAllTreeCategoriesByParentCategoryId(int? parentCategoryId,
            bool showHidden = false)
        {
            string key = string.Format(CATEGORIES_BY_PARENT_CATEGORY_ID_KEY, parentCategoryId, showHidden);

            return _cacheManager.Get(key, () =>
            {
                var query = (from c in _categoryRepository.Table
                             join cc in _categoryCategoryRepository.Table on c.Id equals cc.ChildCategoryId
                             where (showHidden || c.Published) &&
                             !c.Deleted && (parentCategoryId.HasValue ?
                                            cc.ParentCategoryId == parentCategoryId :
                                            cc.ParentCategoryId == null)
                             orderby cc.DisplayOrder
                             select new CategoryTuple { Category = c, CategoryCategoryId = cc.Id });

                var categories = query.ToList();

                return categories;
            });
        }

        /// <summary>
        /// Gets all categories displayed on the home page
        /// </summary>
        /// <returns>Category collection</returns>
        public virtual IList<Category> GetAllCategoriesDisplayedOnHomePageEx()
        {
            var sortedCategories = (from c in _categoryRepository.Table
                                    join cc in _categoryCategoryRepository.Table on c.Id equals cc.ChildCategoryId
                                    where c.Published &&
                                    !c.Deleted &&
                                    c.ShowOnHomePage
                                    orderby cc.DisplayOrder
                                    select c).ToList();

            return sortedCategories;
        }

        /// <summary>
        /// Deletes a category category mapping
        /// </summary>
        /// <param name="categoryCategory">Category category</param>
        public virtual void DeleteCategoryCategory(CategoryCategory categoryCategory)
        {
            if (categoryCategory == null)
                throw new ArgumentNullException("categoryCategory");

            _categoryCategoryRepository.Delete(categoryCategory);

            //cache
            _cacheManager.RemoveByPattern(CATEGORIES_PATTERN_KEY);
            _cacheManager.RemoveByPattern(CATEGORYCATEGORIES_PATTERN_KEY);
            _cacheManager.RemoveByPattern(CATEGORY_ALLPARENTCHILDCATEGORIES_KEY);

            //event notification
            _eventPublisher.EntityDeleted(categoryCategory);
        }

        /// <summary>
        /// Gets parent category category mapping collection
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Category and category mapping collection</returns>
        public virtual IList<CategoryCategory> GetCategoryCategoriesByCategoryId(int categoryId, bool showHidden = false)
        {
            if (categoryId == 0)
                return new List<CategoryCategory>();

            string key = string.Format(CATEGORYCATEGORIES_ALLBYCATEGORYID_KEY, showHidden, categoryId);
            return _cacheManager.Get(key, () =>
            {
                var categoryCategories = (from cc in _categoryCategoryRepository.Table
                                          join c in _categoryRepository.Table on cc.ParentCategoryId equals c.Id into c1
                                          from c2 in c1.DefaultIfEmpty()
                                          where cc.ChildCategoryId == categoryId &&
                                          (c2 == null || (!c2.Deleted && (showHidden || c2.Published)))
                                          orderby cc.DisplayOrder
                                          select cc).ToList();

                return categoryCategories;
            });
        }

        /// <summary>
        /// Gets child category category mapping collection
        /// </summary>
        /// <param name="parentCategoryId">Parent category identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Category and category mapping collection</returns>
        public virtual IList<CategoryCategory> GetCategoryCategoriesByParentCategoryId(int? parentCategoryId, bool showHidden = false)
        {
            string key = string.Format(CATEGORYCATEGORIES_ALLBYPARENTCATEGORYID_KEY, showHidden, parentCategoryId);
            return _cacheManager.Get(key, () =>
            {
                var categoryCategories = (from cc in _categoryCategoryRepository.Table
                                          join c in _categoryRepository.Table on cc.ChildCategoryId equals c.Id into c1
                                          from c2 in c1.DefaultIfEmpty()
                                          where (c2 == null || (!c2.Deleted && (showHidden || c2.Published)))
                                          && (parentCategoryId.HasValue ?
                                                    cc.ParentCategoryId == parentCategoryId :
                                                    cc.ParentCategoryId == null)
                                          orderby cc.DisplayOrder
                                          select cc).ToList();

                return categoryCategories;
            });
        }

        /// <summary>
        /// Gets all the parent and child category branches for a category
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Category collection</returns>
        public IList<Category> GetParentAndChildCategories(int categoryId, bool showHidden = false)
        {
            if (categoryId == 0)
                return null;

            string key = string.Format(CATEGORY_ALLPARENTCHILDCATEGORIES_KEY, showHidden, categoryId);
            return _cacheManager.Get(key, () =>
            {
                Category category = GetCategoryById(categoryId);

                var categories = GetChildren(category).Union(GetParents(category)).ToList();

                return categories.Cast<Category>().ToList();
            });
        }

        private IEnumerable<Category> GetChildren(Category parent)
        {
            yield return parent;

            if (parent != null)
            {
                var query = from c in _categoryRepository.Table
                            join cc in _categoryCategoryRepository.Table on c.Id equals cc.ChildCategoryId
                            where cc.ParentCategoryId == parent.Id
                            select c;

                foreach (var c1 in query)
                    foreach (var c2 in GetChildren(c1))
                        yield return c2;
            }
        }

        private IEnumerable<Category> GetParents(Category child)
        {
            yield return child;

            if (child != null)
            {
                var query = from c in _categoryRepository.Table
                            join cc in _categoryCategoryRepository.Table on c.Id equals cc.ParentCategoryId
                            where cc.ChildCategoryId == child.Id &&
                                  cc.ParentCategoryId != null
                            select c;

                foreach (var c1 in query)
                    foreach (var c2 in GetParents(c1))
                        yield return c2;
            }
        }

        /// <summary>
        /// Gets a category category mapping 
        /// </summary>
        /// <param name="categoryCategoryId">Category category mapping identifier</param>
        /// <returns>Category category mapping</returns>
        public virtual CategoryCategory GetCategoryCategoryById(int categoryCategoryId)
        {
            if (categoryCategoryId == 0)
                return null;

            string key = string.Format(CATEGORYCATEGORIES_BY_ID_KEY, categoryCategoryId);
            return _cacheManager.Get(key, () =>
            {
                return _categoryCategoryRepository.GetById(categoryCategoryId);
            });
        }

        /// <summary>
        /// Inserts a category category mapping
        /// </summary>
        /// <param name="categoryCategory">Category category mapping</param>
        public virtual void InsertCategoryCategory(CategoryCategory categoryCategory)
        {
            if (categoryCategory == null)
                throw new ArgumentNullException("categoryCategory");

            _categoryCategoryRepository.Insert(categoryCategory);

            //cache
            _cacheManager.RemoveByPattern(CATEGORIES_PATTERN_KEY);
            _cacheManager.RemoveByPattern(CATEGORYCATEGORIES_PATTERN_KEY);
            _cacheManager.RemoveByPattern(CATEGORY_ALLPARENTCHILDCATEGORIES_KEY);

            //event notification
            _eventPublisher.EntityInserted(categoryCategory);
        }

        /// <summary>
        /// Updates the category category mapping 
        /// </summary>
        /// <param name="categoryCategory">>Category category mapping</param>
        public virtual void UpdateCategoryCategory(CategoryCategory categoryCategory)
        {
            if (categoryCategory == null)
                throw new ArgumentNullException("categoryCategory");

            _categoryCategoryRepository.Update(categoryCategory);

            //cache
            _cacheManager.RemoveByPattern(CATEGORIES_PATTERN_KEY);
            _cacheManager.RemoveByPattern(CATEGORYCATEGORIES_PATTERN_KEY);
            _cacheManager.RemoveByPattern(CATEGORY_ALLPARENTCHILDCATEGORIES_KEY);

            //event notification
            _eventPublisher.EntityUpdated(categoryCategory);
        }

        #endregion
    }

    public class CategoryTuple
    {
        public Category Category { get; set; }
        public int CategoryCategoryId { get; set; }
    }
}
