using Css.Extensions;
using System;

namespace Css.Syntax;

public class AttributeReferencesFinder(string elementName, string attributeName, Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(CompoundSelectorSyntax node)
    {
        foreach (var child in node.Selectors.AsValueEnumerable())
        {
            if (child is ElementSelectorSyntax elementSelectorSyntax)
            {
                if (!elementSelectorSyntax.NameToken.Value.Equals(elementName, StringComparison.OrdinalIgnoreCase))
                {
                    MarkAsConsumed(node);
                    return;
                }

                var width = 0;
                foreach (var child2 in node.Selectors.AsValueEnumerable())
                {
                    if (child2 is AttributeSelectorSyntax attributeSelectorSyntax)
                    {
                        if (attributeSelectorSyntax.NameToken.Value.Equals(attributeName, StringComparison.OrdinalIgnoreCase))
                        {
                            var match = CreateMatch(attributeSelectorSyntax.NameToken, offset: width + attributeSelectorSyntax.LeadingTrivia.Width() + attributeSelectorSyntax.OpenBracketToken.Width);
                            found(match);
                            break;
                        }
                    }

                    width += child2.Width;
                }

                MarkAsConsumed(node);
                return;
            }
        }

        base.Visit(node);
    }
}
