using System;

namespace Css.Syntax;

public class PseudoClassReferencesFinder(string name, Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(PseudoClassSelectorSyntax node)
    {
        if (node.NameToken.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            Report(node.NameToken, offset: node.ColonToken.Width);
        }

        base.Visit(node);
    }
}
