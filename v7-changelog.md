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
5. Replace all public Task On****() methods in AppBase with event handler pattern. Methods with return values does not seem to fit this pattern
   - OnStartAppEvent (not used today) and OnEndAppEvent are sent to all registered services implementing IAppEventReceiver.
   - OnStartProcessTask, OnEndProcessTask and OnAbandonProcessTask are sent to all registered services implementing ITaskEventReceiver.
6. Move EFormidling logic out of AppBase. Not a separate nuget yet, but moved to a separate namespace
   - SendEFormidlingShipment(Instance instance) method and all related private methods moved to DefaultEFormidlingService.
   - GenerateEFormidlingMetadata(Instance instance) methods is removed and should be implemented by providing a class implementing IEFormidlingMetadata.
   - To make injecting EFormidling services easier a new method is added to Extensions.ServiceCollectionExtensions.
     - AddEFormidlingServices<T>(IConfiguration configuration) where T is the class implementing IEFormidlingMetadata in the serviceowners project. Eg: services.AddEFormidlingServices<Altinn.App.ServiceOwners.MyEFormidlingMetadata>(config). This will register all necessary services.
   - GetEFormidlingReceivers() is overridden by implementing IEFormidlingReceivers and supplied as the second Generic to AddEFromidlingServices, default implementation is used if not supplied. This is used to get the list of services that should receive the EFormidlingShipment.
   - TODO: Test if this logic can be written as a new Event receiver making it easier to extend an application with EFormidling just by including the EFormidling nuget
7. A side effect of 4. 5. and 6. the only methods left in IAltinnApp/AppBase was calls to other services.
   - Replaced calls in the code with direct calls to these services and removed them from AppBase.
   - Removed CanEndProcessTask(....) from AppBase and replaced it with a static method in Helpers.ProcessHelpers (the checks only use the input arguments to the method)
   - No methods are left in IAltinnApp/AppBase. Removed the interface and implementation.