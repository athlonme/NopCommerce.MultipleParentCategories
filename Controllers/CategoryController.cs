using System;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Controllers;
using Nop.Plugin.Misc.MultipleParents.Domain;
using Nop.Plugin.Misc.MultipleParents.Services;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Telerik.Web.Mvc;
using Telerik.Web.Mvc.UI;

namespace Nop.Plugin.Misc.MultipleParents.Controllers
{
    [AdminAuthorize]
    public class CategoryController : BaseNopController
    {
        private readonly CategoryServiceExt _categoryService;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;

        private readonly Nop.Admin.Controllers.CategoryController _categoryController;

        public CategoryController(ICategoryService categoryService, IPermissionService permissionService,
            Nop.Admin.Controllers.CategoryController categoryController, ILocalizationService localizationService)
        {
            this._categoryService = categoryService as CategoryServiceExt;
            this._permissionService = permissionService;
            this._localizationService = localizationService;

            this._categoryController = categoryController;
        }

        #region utilities

        [NonAction]
        private void PrepareCategoryMapping(Nop.Plugin.Misc.MultipleParents.Models.CategoryModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            model.NumberOfAvailableCategories = _categoryService.GetAllCategories(true).Count;
        }

        #endregion

        #region Tree overrides

        public ActionResult Tree()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var rootCategories = _categoryService.GetAllTreeCategoriesByParentCategoryId(null, true);

            return View("~/Plugins/Misc.MultipleParents/Views/Tree.cshtml", rootCategories);
        }

        //ajax
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TreeLoadChildren(TreeViewItem node)
        {
            var id = !string.IsNullOrEmpty(node.Value) ? Convert.ToInt32(node.Value) : 0;

            var children = _categoryService.GetAllTreeCategoriesByParentCategoryId(_categoryService.GetCategoryCategoryById(id).ChildCategoryId, true).Select(x =>
                new TreeViewItem
                {
                    Text = x.Category.Name,
                    Value = x.CategoryCategoryId.ToString(),
                    Url = Url.Action("Edit") + "/" + x.Category.Id,
                    LoadOnDemand = _categoryService.GetCategoryCategoriesByParentCategoryId(x.Category.Id, true).Count > 0,
                    Enabled = true,
                    ImageUrl = Url.Content("~/Administration/Content/images/ico-content.png")
                });

            return new JsonResult { Data = children };
        }

        //ajax
        public ActionResult TreeDrop(int item, int destinationitem, string position)
        {
            var categoryCategoryItem = _categoryService.GetCategoryCategoryById(item);
            var categoryCategoryDestinationItem = _categoryService.GetCategoryCategoryById(destinationitem);

            var oldParentCategoryId = categoryCategoryItem.ParentCategoryId;
            var oldDisplayOrder = categoryCategoryItem.DisplayOrder;

            var categoriesToExclude =
                _categoryService.GetParentAndChildCategories(categoryCategoryItem.ChildCategoryId, true)
                .Where(c => (categoryCategoryItem.ParentCategoryId == null && c != null) || (categoryCategoryItem.ParentCategoryId != null && (c == null || categoryCategoryItem.ParentCategoryId != c.Id)))
                .Select(c => new int?(c != null ? c.Id : 0))
                .ToList();

            switch (position)
            {
                case "over":
                    categoryCategoryItem.ParentCategoryId = categoryCategoryDestinationItem.ChildCategoryId;
                    categoryCategoryItem.DisplayOrder = 1;
                    break;
                case "before":
                    categoryCategoryItem.ParentCategoryId = categoryCategoryDestinationItem.ParentCategoryId;
                    categoryCategoryItem.DisplayOrder = categoryCategoryDestinationItem.DisplayOrder - 1;
                    break;
                case "after":
                    categoryCategoryItem.ParentCategoryId = categoryCategoryDestinationItem.ParentCategoryId;
                    categoryCategoryItem.DisplayOrder = categoryCategoryDestinationItem.DisplayOrder + 1;
                    break;
            }

            // don't update the category category mapping if new parent category causes a circular reference
            if (categoriesToExclude.Contains(categoryCategoryItem.ParentCategoryId))
            {
                categoryCategoryItem.ParentCategoryId = oldParentCategoryId;
                categoryCategoryItem.DisplayOrder = oldDisplayOrder;

                return Json(new { success = false, message = _localizationService.GetResource("Plugin.Misc.MultipleParents.CannotDropHere") });
            }
            else
            {
                _categoryService.UpdateCategoryCategory(categoryCategoryItem);

                return Json(new { success = true });
            }
        }

        #endregion

        #region Create / Edit / Delete overrides

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var model = new Nop.Plugin.Misc.MultipleParents.Models.CategoryModel();

            PrepareCategoryMapping(model);

            var result = _categoryController.Create();
            if (result is ViewResult)
            {
                model.BaseViewModel = ((ViewResult)result).Model as Nop.Admin.Models.Catalog.CategoryModel;
            }
            else
                return result;

            return View("~/Plugins/Misc.MultipleParents/Views/Create.cshtml", model);
        }

        [HttpPost, ParameterBasedOnFormNameAttribute("save-continue", "continueEditing")]
        public ActionResult Create(Nop.Admin.Models.Catalog.CategoryModel model, bool continueEditing)
        {
            var result = _categoryController.Create(model, continueEditing);

            int newCategoryId = _categoryService.GetAllCategories(true)
                                                    .Where(c => c.Name == model.Name)
                                                    .Select(c => c.Id).OrderByDescending(i => i)
                                                    .FirstOrDefault();
            if (newCategoryId > 0)
            {
                // add default category category mapping (root)
                _categoryService.InsertCategoryCategory(
                    new CategoryCategory() 
                    { 
                        ParentCategoryId = null, 
                        ChildCategoryId = newCategoryId, 
                        DisplayOrder = 0 
                    }
                );
            }

            return result;
        }

        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var model = new Nop.Plugin.Misc.MultipleParents.Models.CategoryModel { CategoryId = id };

            PrepareCategoryMapping(model);

            var result = _categoryController.Edit(id);
            if (result is ViewResult)
            {
                model.BaseViewModel = ((ViewResult)result).Model as Nop.Admin.Models.Catalog.CategoryModel;
            }
            else
                return result;

            return View("~/Plugins/Misc.MultipleParents/Views/Edit.cshtml", model);
        }

        [HttpPost, ParameterBasedOnFormNameAttribute("save-continue", "continueEditing")]
        public ActionResult Edit(Nop.Admin.Models.Catalog.CategoryModel model, bool continueEditing)
        {
            return _categoryController.Edit(model, continueEditing);
        }

        #endregion

        #region CategoryCategory

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryCategoryList(GridCommand command, int childCategoryId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var categoryCategories = _categoryService.GetCategoryCategoriesByCategoryId(childCategoryId, showHidden:true);
            var categoryCategoriesModel = categoryCategories
                .Select(x =>
                {
                    return new Nop.Plugin.Misc.MultipleParents.Models.CategoryModel.CategoryCategoryModel()
                    {
                        Id = x.Id,
                        Category = (x.ParentCategoryId.HasValue ?
                            _categoryService.GetCategoryById(x.ParentCategoryId.Value).GetCategoryBreadCrumb(_categoryService) : "[None]"),
                        ParentCategoryId = x.ParentCategoryId,
                        ChildCategoryId = x.ChildCategoryId,
                        DisplayOrder1 = x.DisplayOrder
                    };
                })
                .ToList();

            var model = new GridModel<Nop.Plugin.Misc.MultipleParents.Models.CategoryModel.CategoryCategoryModel>
            {
                Data = categoryCategoriesModel,
                Total = categoryCategoriesModel.Count
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [HttpGet]
        public ActionResult CategoryCategoryGetAvailableCategories(int? currentParentCategoryId, int childCategoryId)
        {
            var categoriesToExclude =
                _categoryService.GetParentAndChildCategories(childCategoryId, true)
                .Where(c => (currentParentCategoryId == null && c != null) || (currentParentCategoryId != null && (c == null || currentParentCategoryId != c.Id)))
                .Select(c => new { Name = (c != null ? c.Name : "[None]"), Id = new int?((c != null ? c.Id : 0)) });

            var categories =
                _categoryService.GetAllCategories(true)
                    .Select(c => new { Name = c.Name, Id = new int?(c.Id) })
                    .Except(categoriesToExclude)
                    .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                    .ToList();

            if (currentParentCategoryId == null || !_categoryService.GetCategoryCategoriesByCategoryId(childCategoryId, true).Any(cc => !cc.ParentCategoryId.HasValue))
                categories.Insert(0, new SelectListItem { Text = "[None]", Value = "0" });

            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryCategoryInsert(GridCommand command, Nop.Plugin.Misc.MultipleParents.Models.CategoryModel.CategoryCategoryModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            int parentCategoryId = 0;

            if (!String.IsNullOrEmpty(model.Category) && Int32.TryParse(model.Category, out parentCategoryId))
            {
                var categoryCategory = new CategoryCategory()
                {
                    ChildCategoryId = model.ChildCategoryId,
                    DisplayOrder = model.DisplayOrder1
                };

                //use Category property (not CategoryId) because appropriate property is stored in it
                categoryCategory.ParentCategoryId = parentCategoryId;
                if (categoryCategory.ParentCategoryId == 0)
                    categoryCategory.ParentCategoryId = null;

                _categoryService.InsertCategoryCategory(categoryCategory);
            }

            return CategoryCategoryList(command, model.ChildCategoryId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryCategoryUpdate(GridCommand command, Nop.Plugin.Misc.MultipleParents.Models.CategoryModel.CategoryCategoryModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var categoryCategory = _categoryService.GetCategoryCategoryById(model.Id);
            if (categoryCategory == null)
                throw new ArgumentException("No category category mapping found with the specified id");

            //use Category property (not CategoryId) because appropriate property is stored in it
            categoryCategory.ParentCategoryId = Int32.Parse(model.Category);
            if (categoryCategory.ParentCategoryId == 0)
                categoryCategory.ParentCategoryId = null;
            categoryCategory.DisplayOrder = model.DisplayOrder1;
            _categoryService.UpdateCategoryCategory(categoryCategory);

            return CategoryCategoryList(command, categoryCategory.ChildCategoryId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryCategoryDelete(int id, GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var categoryCategory = _categoryService.GetCategoryCategoryById(id);
            if (categoryCategory == null)
                throw new ArgumentException("No category category mapping found with the specified id");

            var categoryId = categoryCategory.ChildCategoryId;
            _categoryService.DeleteCategoryCategory(categoryCategory);

            return CategoryCategoryList(command, categoryId);
        }

        #endregion
    }
}
