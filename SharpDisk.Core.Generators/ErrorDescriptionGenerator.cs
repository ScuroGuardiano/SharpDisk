using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SharpDisk.Core.Generators
{
    /// <summary>
    /// Turns <c>[ErrorDescription]</c> attributes on a flags enum into a lookup class.
    /// </summary>
    /// <remarks>
    /// The analyzer hands back a bitmask; a user needs a list of sentences. The generated
    /// <c>Describe</c> method is the bridge - give it the mask, get back one description per bit
    /// that is set, in declaration order, each carrying a <c>Severity</c>.
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed class ErrorDescriptionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var members = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    GeneratorHelpers.ErrorDescriptionAttribute,
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

            // For an enum member the containing type is the enum itself, which is exactly the
            // flags type the generated lookup is keyed by.
            if (container is not { TypeKind: TypeKind.Enum } || container.ContainingNamespace.IsGlobalNamespace)
            {
                return null;
            }

            var attribute = context.Attributes.FirstOrDefault();
            if (attribute is null || attribute.ConstructorArguments.Length != 3)
            {
                return null;
            }

            var name = attribute.ConstructorArguments[0].Value as string;
            var description = attribute.ConstructorArguments[1].Value as string;
            var severity = GeneratorHelpers.EnumValue(attribute.ConstructorArguments[2]);

            var flagsType = GeneratorHelpers.GlobalName(container);
            var infoType = $"global::SharpDisk.Core.Descriptions.ErrorInfo<{flagsType}>";

            var initializer = new StringBuilder()
                .Append("        new ").Append(infoType).Append('(')
                .Append(flagsType).Append('.').Append(symbol.Name).Append(", ")
                .Append("KeyPrefix, ")
                .Append(GeneratorHelpers.Literal(symbol.Name)).Append(", ")
                .Append(GeneratorHelpers.Literal(name)).Append(", ")
                .Append(GeneratorHelpers.Literal(description)).Append(", ")
                .Append(severity)
                .Append("),\n")
                .ToString();

            var (file, position) = GeneratorHelpers.SourceOrder(symbol);

            return new Member(
                container.ContainingNamespace.ToDisplayString(),
                container.Name,
                flagsType,
                initializer,
                file,
                position);
        }

        private static void Emit(SourceProductionContext context, ImmutableArray<Member> members)
        {
            if (members.IsDefaultOrEmpty)
            {
                return;
            }

            var containers = members.GroupBy(static m => (m.Namespace, m.EnumName, m.FlagsType));

            foreach (var container in containers)
            {
                var (ns, enumName, flagsType) = container.Key;
                var className = GeneratorHelpers.DescriptionsClassName(enumName);
                var infoType = $"global::SharpDisk.Core.Descriptions.ErrorInfo<{flagsType}>";

                var initializers = string.Concat(container
                    .OrderBy(static m => m.OrderFile, System.StringComparer.Ordinal)
                    .ThenBy(static m => m.OrderPosition)
                    .Select(static m => m.Initializer));

                var source = GeneratorHelpers.FileHeader(
                    [
                        "System.Collections.Frozen",
                        "System.Collections.Generic",
                        "System.Collections.Immutable",
                        "System.Diagnostics.CodeAnalysis",
                        "System.Linq",
                        "System.Runtime.CompilerServices",
                        "SharpDisk.Core.Localization",
                    ])
                    + $@"
namespace {ns};

/// <summary>
/// Human-readable descriptions of every flag in <see cref=""{enumName}""/>.
/// </summary>
/// <remarks>
/// Generated from the <c>[ErrorDescription]</c> attributes on <see cref=""{enumName}""/>.
/// <see cref=""Describe""/> is the one a UI wants: it expands a mask into the descriptions of the
/// flags actually set.
/// </remarks>
public static class {className}
{{
    /// <summary>
    /// Prefix every translation key in this class starts with.
    /// </summary>
    public const string KeyPrefix = {GeneratorHelpers.Literal(enumName)};

    /// <summary>
    /// Every description, in the order the flags are declared.
    /// </summary>
    public static ImmutableArray<{infoType}> All {{ get; }} =
    [
{initializers}    ];

    /// <summary>
    /// Descriptions indexed by flag.
    /// </summary>
    public static FrozenDictionary<{flagsType}, {infoType}> ByFlag {{ get; }} =
        All.GroupBy(static info => info.Flag)
           .ToFrozenDictionary(static group => group.Key, static group => group.First());

    /// <summary>
    /// Description of a single flag, or <c>null</c> when it has none.
    /// </summary>
    public static {infoType}? Get({flagsType} flag)
        => ByFlag.TryGetValue(flag, out var info) ? info : null;

    /// <inheritdoc cref=""Get""/>
    public static bool TryGet({flagsType} flag, [MaybeNullWhen(false)] out {infoType} info)
        => ByFlag.TryGetValue(flag, out info);

    /// <summary>
    /// Descriptions of every described flag set in <paramref name=""flags""/>, in declaration order.
    /// </summary>
    /// <remarks>
    /// Zero-valued members (the <c>None</c> of a flags enum) are skipped - every mask contains
    /// them, so reporting them would be noise.
    /// </remarks>
    public static IEnumerable<{infoType}> Describe({flagsType} flags)
    {{
        foreach (var info in All)
        {{
            if (info.Flag != default && (flags & info.Flag) == info.Flag)
            {{
                yield return info;
            }}
        }}
    }}

    /// <summary>
    /// The worst severity among the flags set in <paramref name=""flags""/>, or <c>null</c> when
    /// none of them is described. What decides whether a dialog says note, warning or stop.
    /// </summary>
    public static global::SharpDisk.Core.Severity? MaxSeverity({flagsType} flags)
    {{
        global::SharpDisk.Core.Severity? worst = null;

        foreach (var info in Describe(flags))
        {{
            if (worst is null || info.Severity > worst)
            {{
                worst = info.Severity;
            }}
        }}

        return worst;
    }}

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
            string EnumName,
            string FlagsType,
            string Initializer,
            string OrderFile,
            int OrderPosition);
    }
}
