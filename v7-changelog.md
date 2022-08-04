1. Added [packages diagram](packages.drawio.svg) to visualize new package structure
2. Merged `Altinn.App.Common` and `Altinn.App.PlatformServices` into `Altinn.App.Core`
   - Kept namespacesd and folder structure
   - Consolidated all Nuget packages
   - Removed support for .Net5.0
3. Moved and grouped http clients into new namespaces
   - From Implementation folder to Altinn.App.Core.Infrastructure.Clients.[Area] where area is: Register, Storage, Profile, Authorization, Authentication, Events, Pdf, KeyVault
   - Not named HttpClients since clients might be other than http.
   - ProcessAppSI
4. Replaced virtual/abstract methods in AppBase with Dependency Injection to implement custom code.
   - GetAppModelType() and CreateNewAppModel() is replaced by new class in the App that implements IAppModel (This file should be the only dotnet code needed by default for an app). Remove the inheritence and implementation of IAltinnApp. Remove the call to base constructor. Add IAppModel.
   - Overriding ProcessDataWrite/Read is now done by injecting a class implementing IDataProcessor. `App/logic/DataProcessing/DataProcessingHandler.cs` in app-template is no longer needed. Add IDataProcessor to DataProcessingHandler.cs if you have custom code there. Register the implemenation in program.cs `services.AddTransient<IDataProcessor, DataProcessingHandler>();`
   - Overriding RunInstantiationValidation and DataCreation is now done by injecting a class implementing IInstantiation. `App/logic/InstantiationHandler.cs` in app-template is no longer needed. Add IInstantiation to InstantiationHandler.cs if you have custom code there. Rename the method RunInstantiationValidation to Validation. Register the implementation in program.cs `services.AddTransient<IInstantiation, InstantiationHandler>();`. Remove the method from App.cs.
   - Overriding validation logic is done by injecting a class implementing IInstanceValidator. `App/logic/Validation/ValidationHandler.cs` in app-template is no longer needed. Add IInstanceValidator to ValidationHandler.cs if you have custom code there. Register the implementation in program.cs `services.AddTransient<IInstanceValidator, ValidationHandler>();`. 
   - ICustomPdfGenerator renamed to IPdfFormatter, ICustomPdfGenerator was already implemented with DI, `App/logic/Print/PdfHandler.cs` in app-template is no longer needed.
   - Deprecated method `IAltinnApp.GetPageOrder()` is removed. It's now only possible to override this logic by injecting a class implementing IPageOrder
   - Overriding logic for RunProcessTaskEnd is done by injecting a class implementing ITaskProcessor.
5. Created AddAltinnAppServices in Api project as the new main method to call from Program.cs in the app
6. Moved code
   - Moved registration of Application Insights from Core to Api project.
   - Moved Filters to Infrastructure namespace in Api project
   - Moved SecurityHeaders middleware to Infrastructure namespace in Api project