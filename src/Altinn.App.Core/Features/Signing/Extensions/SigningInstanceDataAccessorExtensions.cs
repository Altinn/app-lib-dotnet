using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Extensions;

/// <summary>
/// Extension methods for <see cref="IInstanceDataAccessor"/> used in signing.
/// </summary>
internal static class SigningInstanceDataAccessorExtensions
{
    /// <summary>
    /// Set service owner as authentication method for the given data types.
    /// </summary>
    public static void SetServiceOwnerAuthForRestrictedDataTypes(
        this IInstanceDataAccessor accessor,
        ApplicationMetadata appMetadata,
        string?[] dataTypeIds
    )
    {
        IEnumerable<DataType> signatureDataTypes = GetDataTypes(appMetadata, dataTypeIds);

        foreach (DataType signatureDataType in signatureDataTypes)
        {
            if (IsRestrictedDataType(signatureDataType))
            {
                accessor.SetAuthenticationMethod(signatureDataType, StorageAuthenticationMethod.ServiceOwner());
            }
        }
    }

    private static IEnumerable<DataType> GetDataTypes(
        ApplicationMetadata appMetadata,
        IEnumerable<string?> dataTypeIds
    ) => dataTypeIds.Select(dataTypeId => GetDataType(appMetadata, dataTypeId)).OfType<DataType>();

    private static DataType? GetDataType(ApplicationMetadata appMetadata, string? dataTypeId) =>
        dataTypeId is null
            ? null
            : appMetadata.DataTypes.FirstOrDefault(x => x.Id.Equals(dataTypeId, StringComparison.OrdinalIgnoreCase));

    private static bool IsRestrictedDataType([NotNullWhen(true)] DataType? dataType) =>
        !string.IsNullOrWhiteSpace(dataType?.ActionRequiredToRead)
        || !string.IsNullOrWhiteSpace(dataType?.ActionRequiredToWrite);
}
