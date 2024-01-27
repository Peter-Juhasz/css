using Css.Source;
using EditorTest.Data;
using EditorTest.Extensions;
using EditorTest.Syntax;
using System;

namespace EditorTest.Outlining;

public class OutliningSyntaxWalker(Action<SourceSpan> found) : SyntaxLocatorWalker
{
    public override void Visit(RuleDeclarationSyntax node)
    {
        int relativeOffset = node.LeadingTrivia.Width() + node.Selectors.Width + node.OpenBrace.Width;
        string? lastPropertyName = null;
        int relativeCollapseStart = 0, relativeCollapseEnd = 0; 

        foreach (var item in node.Nodes.Items.AsValueEnumerable())
        {
            if (item is not PropertySyntax property)
            {
                lastPropertyName = null;
                relativeCollapseStart = relativeCollapseEnd = 0;
                relativeOffset += item.Width;
                continue;
            }

            var name = property.NameSyntax.NameToken.Value;
            var isVariable = name.StartsWith("--", StringComparison.Ordinal);
            var isVendorSpecific = !isVariable && name.StartsWith("-", StringComparison.Ordinal);
            var isVendorContinuation = lastPropertyName != null && CssWebData.Index.VendorPrefixes.Any(p =>
                name.Length == p.Length + lastPropertyName.Length && name.EndsWith(lastPropertyName, StringComparison.Ordinal) && name.StartsWith(p, StringComparison.Ordinal)
            );

            if (isVendorContinuation)
            {
                relativeCollapseEnd = relativeOffset + item.Width - item.TrailingTrivia.Width();
            }
            else
            {
                if (relativeCollapseStart != default && relativeCollapseStart < relativeCollapseEnd && relativeCollapseEnd < relativeOffset)
                {
                    found(SourceSpan.FromBounds(Consumed + relativeCollapseStart, Consumed + relativeCollapseEnd));
                }

                if (!isVariable && !isVendorSpecific)
                {
                    lastPropertyName = name;
                    relativeCollapseStart = relativeCollapseEnd = relativeOffset + item.Width - item.TrailingTrivia.Width();
                }
                else
                {
                    lastPropertyName = null;
                    relativeCollapseStart = relativeCollapseEnd = 0;
                }
            }

            relativeOffset += item.Width;
        }

        base.Visit(node);
    }
}
