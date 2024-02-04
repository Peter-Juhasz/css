using Css.Source;
using Css.Data;
using System;

namespace Css.Syntax;

public class ColorReferencesFinder(ColorData color, Action<SourceSpan> found) : SyntaxLocatorWalker
{
    public override void Visit(HexColorToken node)
    {
        if (node.Value.Equals(color.Hex, StringComparison.OrdinalIgnoreCase))
        {
            found(new(Consumed, node.Width));
        }

        base.Visit(node);
    }


    private bool _searchValues = false;

    public override void Visit(PropertySyntax node)
    {
        var isVariable = node.IsVariable;
        var canContainColorByDefinition = CssWebData.Index.Properties.TryGetValue(node.NameToken.Value, out var definition) &&
            definition.Restrictions != null && definition.Restrictions.Contains("color");
        var match = isVariable || canContainColorByDefinition;

        if (!match)
        {
            MarkAsConsumed(node);
            return;
        }

        _searchValues = true;
        base.Visit(node);
        _searchValues = false;
    }

    public override void Visit(IdentifierExpressionSyntax node)
    {
        if (_searchValues)
        {
            if (node.NameToken.Value.Equals(color.Name, StringComparison.OrdinalIgnoreCase))
            {
                found(new(Consumed + node.LeadingTrivia.Width(), node.NameToken.Width));
            }
        }

        base.Visit(node);
    }

    public override void Visit(FunctionCallExpressionSyntax node)
    {
        if (node.NameToken.Value.Equals("rgb", StringComparison.OrdinalIgnoreCase))
        {
            //found(new(Consumed + node.LeadingTrivia.Width(), node.NameToken.Width));
        }

        base.Visit(node);
    }
}
