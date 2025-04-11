namespace HemSoft.AI;

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;

/// <summary>
/// Base class for implementing AI tools
/// </summary>
public abstract class AIToolBase
{
    /// <summary>
    /// Creates a function tool definition
    /// </summary>
    /// <param name="name">The name of the tool</param>
    /// <param name="description">The description of the tool</param>
    /// <param name="parameterSchema">The JSON schema for the tool parameters</param>
    /// <returns>A function tool definition</returns>
    public static ChatCompletionsFunctionToolDefinition CreateFunctionTool(string name, string description, string parameterSchema)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(description);

        return new ChatCompletionsFunctionToolDefinition
        {
            Name = name,
            Description = description,
            Parameters = BinaryData.FromString(parameterSchema)
        };
    }

    /// <summary>
    /// Creates a function tool definition from a type
    /// </summary>
    /// <typeparam name="T">The type to create a schema from</typeparam>
    /// <param name="name">The name of the tool</param>
    /// <param name="description">The description of the tool</param>
    /// <returns>A function tool definition</returns>
    public static ChatCompletionsFunctionToolDefinition CreateFunctionToolFromType<T>(string name, string description)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(description);

        var schema = JsonSerializer.SerializeToDocument(new
        {
            type = "object",
            properties = typeof(T).GetProperties().ToDictionary(
                p => p.Name,
                p => new { type = GetJsonType(p.PropertyType) }
            ),
            required = typeof(T).GetProperties().Where(p => !IsNullable(p.PropertyType)).Select(p => p.Name).ToArray()
        });

        return new ChatCompletionsFunctionToolDefinition
        {
            Name = name,
            Description = description,
            Parameters = BinaryData.FromString(schema.RootElement.ToString())
        };
    }

    private static string GetJsonType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return "integer";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            return "array";
        return "object";
    }

    private static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }


}
