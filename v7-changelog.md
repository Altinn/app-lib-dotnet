1. Added [packages diagram](packages.drawio.svg) to visualize new package structure
2. Merged `Altinn.App.Common` and `Altinn.App.PlatformServices` into `Altinn.App.Core`
   - Kept namespacesd and folder structure
   - Consolidated all Nuget packages
   - Removed support for .Net5.0
3. Moved and grouped http clients into new namespaces
   - From Implementation folder to Altinn.App.Core.Infrastructure.Clients.[Area] where area is Register, Storage
   - Not named HttpClients since clients might be other than http.
4. Replaced virtual/abstract methods in AppBase with Dependency Injection to implement custom code.
   - GetAppModelType() and CreateNewAppModel() is replaced by new class in the App that implements IAppModel (This file should be the only dotnet code needed by default for an app)
   - Overriding ProcessDataWrite/Read is now done by injecting a class implementing IDataProcessor. `App/logic/DataProcessing/DataProcessingHandler.cs` in app-template is no longer needed
   - Overriding RunInstantiationValidation and DataCreation is now done by injecting a class implementing IInstantiation. `App/logic/InstantiationHandler.cs` in app-template is no longer needed.
   - Overriding validation logic is done by injecting a class implementing IInstanceValidation. `App/logic/Validation/ValidationHandler.cs` in app-template is no longer needed.
   - ICustomPdfGenerator renamed to IPdfFormatter, ICustomPdfGenerator was already implemented with DI, `App/logic/Print/PdfHandler.cs` in app-template is no longer needed.
   - Deprecated method `IAltinnApp.GetPageOrder()` is removed. It's now only possible to override this logic by injecting a class implementing IPageOrder
   - Overriding logic for RunProcessTaskEnd is done by injecting a class implementing ITaskProcessor.  