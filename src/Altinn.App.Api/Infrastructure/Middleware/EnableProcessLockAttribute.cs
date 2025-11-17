namespace Altinn.App.Api.Infrastructure.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
internal sealed class EnableProcessLockAttribute : Attribute;
