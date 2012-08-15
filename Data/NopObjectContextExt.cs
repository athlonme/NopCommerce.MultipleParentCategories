using Nop.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Nop.Plugin.Misc.MultipleParents.Data
{
    public class NopObjectContextExt : NopObjectContext, IDbContext
    {
        public NopObjectContextExt(string nameOrConnectionString) : base(nameOrConnectionString) { }

        /// <summary>
        /// This method is called when the model for a derived context has been initialized, but
        /// before the model has been locked down and used to initialize the context.  The default
        /// implementation of this method does nothing, but it can be overridden in a derived class
        /// such that the model can be further configured before it is locked down.
        /// </summary>
        /// <param name="modelBuilder">The builder that defines the model for the context being created.</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new CategoryCategoryMap());

            base.OnModelCreating(modelBuilder);            
        }

        /// <summary>
        /// Creates the database script.
        /// </summary>
        /// <returns></returns>
        public new string CreateDatabaseScript()
        {
            // only get script for new Category_Category_Mapping table
            string script = ((IObjectContextAdapter)this).ObjectContext.CreateDatabaseScript();
            int startIndex = script.IndexOf("create table [dbo].[Category_Category_Mapping]");
            int stopIndex = script.IndexOf("create table", startIndex + 12);

            return script.Substring(startIndex, stopIndex - startIndex);
        }

        /// <summary>
        /// Installs the database schema.
        /// </summary>
        public void Install()
        {
            //It's required to set initializer to null (for SQL Server Compact).
            //otherwise, you'll get something like "The model backing the 'your context name' context has changed since the database was created. Consider using Code First Migrations to update the database"
            Database.SetInitializer<NopObjectContextExt>(null);

            //create the table
            var dbScript = CreateDatabaseScript();
            Database.ExecuteSqlCommand(dbScript);

            //transfer existing parent categories into new table
            var dbTransferScript = "INSERT INTO " + CategoryCategoryMap.TableName + " (ParentCategoryId, ChildCategoryId, DisplayOrder) SELECT CASE WHEN ParentCategoryId = 0 THEN NULL ELSE ParentCategoryId END, Id, DisplayOrder FROM Category";
            Database.ExecuteSqlCommand(dbTransferScript);

            //add translateable resources
            var dbResourceScript = @"
                declare @resources xml                
                set @resources='
                <Language>
                  <LocaleResource Name=""Plugin.Misc.MultipleParents.CannotDropHere"">
                    <Value>Category cannot be dropped here as it already exists in one of its parents.</Value>
                  </LocaleResource>
                  <LocaleResource Name=""Plugin.Misc.MultipleParents.Fields.Category"">
                    <Value>Category</Value>
                  </LocaleResource>
                  <LocaleResource Name=""Plugin.Misc.MultipleParents.Fields.DisplayOrder"">
                    <Value>Display Order</Value>
                  </LocaleResource>
                  <LocaleResource Name=""Plugin.Misc.MultipleParents.Categories"">
                    <Value>Parent categories</Value>
                  </LocaleResource>
                  <LocaleResource Name=""Plugin.Misc.MultipleParents.NoCategoriesAvailable"">
                    <Value>No parent categories available</Value>
                  </LocaleResource>
                  <LocaleResource Name=""Plugin.Misc.MultipleParents.SaveBeforeEdit"">
                    <Value>You need to save the category before you can add parent categories for this category.</Value>
                  </LocaleResource>
                </Language>'

                CREATE TABLE #LocaleStringResourceTmp
	            (
		            [ResourceName] [nvarchar](200) NOT NULL,
		            [ResourceValue] [nvarchar](max) NOT NULL
	            )

                INSERT INTO #LocaleStringResourceTmp (ResourceName, ResourceValue)
                SELECT	nref.value('@Name', 'nvarchar(200)'), nref.value('Value[1]', 'nvarchar(MAX)')
                FROM	@resources.nodes('//Language/LocaleResource') AS R(nref)

                --do it for each existing language
                DECLARE @ExistingLanguageID int
                DECLARE cur_existinglanguage CURSOR FOR
                SELECT [ID]
                FROM [Language]
                OPEN cur_existinglanguage
                FETCH NEXT FROM cur_existinglanguage INTO @ExistingLanguageID
                WHILE @@FETCH_STATUS = 0
                BEGIN
	                DECLARE @ResourceName nvarchar(200)
	                DECLARE @ResourceValue nvarchar(MAX)
	                DECLARE cur_localeresource CURSOR FOR
	                SELECT ResourceName, ResourceValue
	                FROM #LocaleStringResourceTmp
	                OPEN cur_localeresource
	                FETCH NEXT FROM cur_localeresource INTO @ResourceName, @ResourceValue
	                WHILE @@FETCH_STATUS = 0
	                BEGIN
		                IF (EXISTS (SELECT 1 FROM [LocaleStringResource] WHERE LanguageID=@ExistingLanguageID AND ResourceName=@ResourceName))
		                BEGIN
			                UPDATE [LocaleStringResource]
			                SET [ResourceValue]=@ResourceValue
			                WHERE LanguageID=@ExistingLanguageID AND ResourceName=@ResourceName
		                END
		                ELSE 
		                BEGIN
			                INSERT INTO [LocaleStringResource]
			                (
				                [LanguageID],
				                [ResourceName],
				                [ResourceValue]
			                )
			                VALUES
			                (
				                @ExistingLanguageID,
				                @ResourceName,
				                @ResourceValue
			                )
		                END
		
		                IF (@ResourceValue is null or @ResourceValue = '')
		                BEGIN
			                DELETE [LocaleStringResource]
			                WHERE LanguageID=@ExistingLanguageID AND ResourceName=@ResourceName
		                END
		
		                FETCH NEXT FROM cur_localeresource INTO @ResourceName, @ResourceValue
	                END
	                CLOSE cur_localeresource
	                DEALLOCATE cur_localeresource


	                --fetch next language identifier
	                FETCH NEXT FROM cur_existinglanguage INTO @ExistingLanguageID
                END
                CLOSE cur_existinglanguage
                DEALLOCATE cur_existinglanguage

                DROP TABLE #LocaleStringResourceTmp
            ";
            Database.ExecuteSqlCommand(dbResourceScript);

            SaveChanges();
        }

        /// <summary>
        /// Uninstalls the database schema .
        /// </summary>
        public void Uninstall()
        {
            //drop the table
            Database.ExecuteSqlCommand("DROP TABLE " + CategoryCategoryMap.TableName);
            SaveChanges();
        }
    }
}
