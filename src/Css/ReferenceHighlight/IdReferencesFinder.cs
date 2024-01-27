using Css.Source;
using System;

namespace EditorTest.Syntax;

public class IdReferencesFinder(string name, Action<SourceSpan> found) : SyntaxLocatorWalker
{
    public override void Visit(IdSelectorSyntax node)
    {
        if (node.NameToken.Value == name)
        {
            found(new(Consumed + node.HashToken.Width, node.NameToken.Width));
        }

        base.Visit(node);
    }

    public override void Visit(AttributeSelectorSyntax node)
    {
        if (node.NameToken.Value.Equals("id", StringComparison.OrdinalIgnoreCase) && node.OperatorToken?.Text == "=" && node.ValueToken?.Value == name)
        {
            found(new(Consumed +
                node.LeadingTrivia.Width() +
                node.OpenBracketToken.Width +
                node.NameToken.Width +
                node.OperatorToken.Width + 
                (node.ValueToken is ImplicitStringToken ? 0 : 1),
                node.ValueToken.Width - (node.ValueToken is QuotedStringToken quoted ? (quoted.IsClosed ? 2 : 1) : 0)
            ));
        }

        base.Visit(node);
    }
}
