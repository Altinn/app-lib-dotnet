// using System.Reflection;
// using Altinn.App.Core.Internal.AppModel;
//
// namespace Altinn.App.Integration;
//
// public class TestAppModel<TAssemblyMarker> : IAppModel
// {
//     public object Create(string classRef)
//     {
//         return Activator.CreateInstance(GetModelType(classRef))!;
//     }
//
//     public Type GetModelType(string classRef)
//     {
//         // The default implementations uses the executing assembly, but this does not work in the test project.
//         return Assembly.GetAssembly(typeof(TAssemblyMarker))!.GetType(classRef, true);
//     }
// }
