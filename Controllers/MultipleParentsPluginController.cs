using System.Web.Mvc;
using Nop.Plugin.Misc.MultipleParents.Models;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Misc.MultipleParents.Controllers
{
    [AdminAuthorize]
    public class MultipleParentsPluginController : Controller
    {
        public ActionResult Configure()
        {
            ConfigurationModel model = new ConfigurationModel();

            return View("~/Plugins/Misc.MultipleParents/Views/Configure.cshtml", model);
        }
    }
}
