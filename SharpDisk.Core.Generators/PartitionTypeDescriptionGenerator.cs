using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SharpDisk.Core.Generators
{
    /// <summary>
    /// Turns <c>[PartitionTypeDescription]</c> attributes into a lookup class per container.
    /// </summary>
    /// <remarks>
    /// <c>MbrPartitionTypes</c> and <c>GptPartitionTypes</c> declare a couple of hundred values
    /// between them, and every one needs a display name, an explanation, and a flag saying whether
    /// a user would ever pick it. Keeping that next to the value as an attribute means one place
    /// to edit; this generator is what turns it back into something indexable at runtime.
    /// <para>
    /// The generated class references the original fields by name rather than copying their
    /// values, so a changed constant cannot drift out of sync with its description.
    /// </para>
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed class PartitionTypeDescriptionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var members = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    GeneratorHelpers.PartitionTypeDescriptionAttribute,
                    predicate: static (_, _) => true,
                    transform: static (ctx, _) => Transform(ctx))
                .Where(static member => member is not null)
                .Select(static (member, _) => member!);

            context.RegisterSourceOutput(members.Collect(), static (spc, all) => Emit(spc, all));
        }

        private static Member? Transform(GeneratorAttributeSyntaxContext context)
        {
            var symbol = context.TargetSymbol;
            var container = symbol.ContainingType;
            var valueType = GeneratorHelpers.MemberType(symbol);

            if (container is null || valueType is null || container.ContainingNamespace.IsGlobalNamespace)
            {
                return null;
            }

            var containerName = container.Name;
            var valueTypeName = GeneratorHelpers.GlobalName(valueType);
            var infoType = $"global::SharpDisk.Core.Descriptions.PartitionTypeInfo<{valueTypeName}>";
            var valueReference = $"{GeneratorHelpers.GlobalName(container)}.{symbol.Name}";

            var builder = new StringBuilder();
            var ordinal = 0;

            foreach (var attribute in context.Attributes)
            {
                // (string type, string description, bool common) - anything else means the
                // attribute was edited without this generator being updated to match.
                if (attribute.ConstructorArguments.Length != 3)
                {
                    continue;
                }

                var type = attribute.ConstructorArguments[0].Value as string;
                var description = attribute.ConstructorArguments[1].Value as string;
                var common = attribute.ConstructorArguments[2].Value is true;

                builder
                    .Append("        new ").Append(infoType).Append('(')
                    .Append(valueReference).Append(", ")
                    .Append("KeyPrefix, ")
                    .Append(GeneratorHelpers.Literal(symbol.Name)).Append(", ")
                    .Append(ordinal).Append(", ")
                    .Append(GeneratorHelpers.Literal(type)).Append(", ")
                    .Append(GeneratorHelpers.Literal(description)).Append(", ")
                    .Append(common ? "true" : "false")
                    .Append("),\n");

                ordinal++;
            }

            if (builder.Length == 0)
            {
                return null;
            }

            var (file, position) = GeneratorHelpers.SourceOrder(symbol);

            return new Member(
                container.ContainingNamespace.ToDisplayString(),
                containerName,
                valueTypeName,
                builder.ToString(),
                file,
                position);
        }

        private static void Emit(SourceProductionContext context, ImmutableArray<Member> members)
        {
            if (members.IsDefaultOrEmpty)
            {
                return;
            }

            var containers = members.GroupBy(static m => (m.Namespace, m.ContainerName, m.ValueType));

            foreach (var container in containers)
            {
                var (ns, containerName, valueType) = container.Key;
                var className = GeneratorHelpers.DescriptionsClassName(containerName);
                var infoType = $"global::SharpDisk.Core.Descriptions.PartitionTypeInfo<{valueType}>";

                var initializers = string.Concat(container
                    .OrderBy(static m => m.OrderFile, System.StringComparer.Ordinal)
                    .ThenBy(static m => m.OrderPosition)
                    .Select(static m => m.Initializers));

                var source = GeneratorHelpers.FileHeader(
                    [
                        "System.Collections.Frozen",
                        "System.Collections.Immutable",
                        "System.Linq",
                        "System.Runtime.CompilerServices",
                        "SharpDisk.Core.Localization",
                    ])
                    + $@"
namespace {ns};

/// <summary>
/// Human-readable descriptions of every value in <see cref=""{containerName}""/>.
/// </summary>
/// <remarks>
/// Generated from the <c>[PartitionTypeDescription]</c> attributes on <see cref=""{containerName}""/>.
/// A value can have more than one meaning, so lookups return a list with the most likely meaning
/// first; <see cref=""GetPrimary""/> is the shortcut when only one can be shown.
/// </remarks>
public static class {className}
{{
    /// <summary>
    /// Prefix every translation key in this class starts with.
    /// </summary>
    public const string KeyPrefix = {GeneratorHelpers.Literal(containerName)};

    /// <summary>
    /// Every description, in the order the values are declared.
    /// </summary>
    public static ImmutableArray<{infoType}> All {{ get; }} =
    [
{initializers}    ];

    /// <summary>
    /// Descriptions indexed by value. Values claimed by several vendors map to several entries.
    /// </summary>
    public static FrozenDictionary<{valueType}, ImmutableArray<{infoType}>> ByValue {{ get; }} =
        All.GroupBy(static info => info.Value)
           .ToFrozenDictionary(static group => group.Key, static group => group.ToImmutableArray());

    /// <summary>
    /// Only the types a user would realistically pick today - what a type picker should list.
    /// </summary>
    public static ImmutableArray<{infoType}> Common {{ get; }} =
        [.. All.Where(static info => info.Common)];

    /// <summary>
    /// Every meaning of <paramref name=""value""/>, or an empty array when nothing describes it.
    /// </summary>
    public static ImmutableArray<{infoType}> Get({valueType} value)
        => ByValue.TryGetValue(value, out var infos) ? infos : [];

    /// <summary>
    /// The most likely meaning of <paramref name=""value""/>, or <c>null</c> when nothing describes it.
    /// </summary>
    public static {infoType}? GetPrimary({valueType} value)
        => ByValue.TryGetValue(value, out var infos) && infos.Length > 0 ? infos[0] : null;

    /// <summary>
    /// Whether <paramref name=""value""/> has any description at all.
    /// </summary>
    public static bool TryGet({valueType} value, out ImmutableArray<{infoType}> infos)
        => ByValue.TryGetValue(value, out infos);

    /// <summary>
    /// Every translatable string in this class, for <see cref=""TranslationCatalog""/>.
    /// </summary>
    public static ImmutableArray<TranslationEntry> TranslationEntries
        => [.. All.SelectMany(static info => info.TranslationEntries)];

    [ModuleInitializer]
    internal static void RegisterTranslations()
        => TranslationCatalog.Register(KeyPrefix, static () => TranslationEntries);
}}
";

                context.AddSource($"{ns}.{className}.g.cs", source);
            }
        }

        private sealed record Member(
            string Namespace,
            string ContainerName,
            string ValueType,
            string Initializers,
            string OrderFile,
            int OrderPosition);
    }
}
