1. Added [packages diagram](packages.drawio.svg) to visualize new package structure
2. Merged `Altinn.App.Commom` and `Altinn.App.PlatformServices` into `Altinn.App.Core`
   - Kept namespacesd and folder structure
   - Consolidated all Nuget packages
   - Removed support for .Net5.0
3. Moved and grouped http clients into new namespaces
   - From Implementation folder to Altinn.App.Core.Infrastructure.Clients.[Area] where area is Register, Storage
   - Not named HttpClients since clients might be other than http.