using Autofac;
using Autofac.Integration.Mvc;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Misc.MultipleParents.Data;
using Nop.Plugin.Misc.MultipleParents.Domain;
using Nop.Plugin.Misc.MultipleParents.Services;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.MultipleParents
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Registers the specified builder.
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="typeFinder">The type finder</param>
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {
            builder
                .RegisterType<CategoryServiceExt>()
                .As<ICategoryService>()
                .InstancePerHttpRequest();

            // register object context
            RegisterObjectContext(builder);

            // register repository
            builder
                .RegisterType<EfRepository<CategoryCategory>>()
                .As<IRepository<CategoryCategory>>()
                .InstancePerHttpRequest();
        }

        /// <summary>
        /// Registers the object context.
        /// </summary>
        /// <param name="builder">The builder</param>
        private void RegisterObjectContext(ContainerBuilder builder)
        {
            // open the data settings manager
            var dataSettingsManager = new DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();

            string nameOrConnectionString = null;

            if (dataProviderSettings != null && dataProviderSettings.IsValid())
            {
                // determine if the connection string exists
                nameOrConnectionString = dataProviderSettings.DataConnectionString;
            }

            // register the named instance
            builder
                .Register<IDbContext>(c => new NopObjectContextExt(nameOrConnectionString ?? c.Resolve<DataSettings>().DataConnectionString))
                .InstancePerHttpRequest();

            // register the type
            builder
                .Register(c => new NopObjectContextExt(nameOrConnectionString ?? c.Resolve<DataSettings>().DataConnectionString))
                .InstancePerHttpRequest();
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
