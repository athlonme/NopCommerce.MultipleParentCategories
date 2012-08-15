using System.Web.Routing;
using Nop.Core.Plugins;
using Nop.Plugin.Misc.MultipleParents.Data;
using Nop.Services.Common;

namespace Nop.Plugin.Misc.MultipleParents
{
    public class MultipleParentsPlugin : BasePlugin, IMiscPlugin
    {
        #region Properties

        private readonly NopObjectContextExt _context;

        #endregion

        #region Ctor

        public MultipleParentsPlugin(NopObjectContextExt context)
        {
            _context = context;
        }

        #endregion

        #region Methods

        public override void Install()
        {
            _context.Install();
            base.Install();
        }

        public override void Uninstall()
        {
            _context.Uninstall();
            base.Uninstall();
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            // no configuration required
            actionName = null;
            controllerName = null;
            routeValues = null;
        }

        #endregion
    }
}
