using System.Text.Json;
using System.Text.Json.Serialization;

namespace Harbor.Data.Json;

/// <summary>
/// Options <see cref="JsonSerializerOptions"/> partagées par les repositories
/// pour la sérialisation des champs JSON (connection_json, auth_json,
/// env_vars_json, variables_json, metadata_json).
/// </summary>
internal static class HarborJsonOptions
{
    /// <summary>
    /// Options par défaut : camelCase, ignore les valeurs <c>null</c>, pas
    /// d'indentation (compact pour le stockage).
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
