using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Themes;

namespace Nop.Plugin.Misc.MultipleParents
{
    public class MultipleParentsAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            // plugin configure route
            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.Configure",
                "Plugins/MultipleParents/Configure",
                new { controller = "MultipleParents", action = "Configure" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            // 'overrides' for default category routes
            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.Create",
                "Admin/Category/Create/",
                new { controller = "Category", action = "Create", area = "Admin" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.Edit",
                "Admin/Category/Edit/{id}",
                new { controller = "Category", action = "Edit", area = "Admin" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.Tree",
                "Admin/Category/Tree/",
                new { controller = "Category", action = "Tree" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.TreeLoadChildren",
                "Admin/Category/TreeLoadChildren/",
                new { controller = "Category", action = "TreeLoadChildren" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.TreeDrop",
                "Admin/Category/TreeDrop/",
                new { controller = "Category", action = "TreeDrop" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            // routes for the new controller actions
            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.CategoryCategoryList",
                "Admin/Category/CategoryCategoryList/{id}",
                new { controller = "Category", action = "CategoryCategoryList" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.CategoryCategoryInsert",
                "Admin/Category/CategoryCategoryInsert/{id}",
                new { controller = "Category", action = "CategoryCategoryInsert" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.CategoryCategoryUpdate",
                "Admin/Category/CategoryCategoryUpdate/{id}",
                new { controller = "Category", action = "CategoryCategoryUpdate" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.CategoryCategoryDelete",
                "Admin/Category/CategoryCategoryDelete/{id}",
                new { controller = "Category", action = "CategoryCategoryDelete" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            InsertRoute(context.Routes, context.MapRoute(
                "Plugin.Misc.MultipleParents.CategoryCategoryGetAvailableCategories",
                "Admin/Category/CategoryCategoryGetAvailableCategories/{id}",
                new { controller = "Category", action = "CategoryCategoryGetAvailableCategories" },
                new[] { "Nop.Plugin.Misc.MultipleParents.Controllers" }
            ));

            ViewEngines.Engines.Clear();

            var themeEngine = new ThemeableRazorViewEngine();

            themeEngine.PartialViewLocationFormats = new string[] {
                // add view path for plugin
                "~/Plugins/Misc.MultipleParents/Views/{0}.cshtml"
            }.Union(themeEngine.PartialViewLocationFormats).ToArray<string>();

            ViewEngines.Engines.Add(themeEngine);
        }

        private void InsertRoute(RouteCollection routes, RouteBase route)
        {
            routes.Remove(route);
            routes.Insert(0, route);
        }
    }
}
