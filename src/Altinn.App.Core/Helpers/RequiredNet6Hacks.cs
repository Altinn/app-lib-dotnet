// This file is a hack for required properties to work in net6.0
// Should be removed when support for targeting net6.0 is removed

#if NET6_0
#pragma warning disable
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Property)]
public class RequiredMemberAttribute : Attribute { }

[AttributeUsage(AttributeTargets.All)]
public class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string name) { }
}
[System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class SetsRequiredMembersAttribute : Attribute { }
#endif