using System;

namespace EditorTest.Syntax;

public class PseudoElementReferencesFinder(string name, Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(PseudoElementSelectorSyntax node)
    {
        if (node.NameToken.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            Report(node.NameToken, offset: node.ColonsToken.Width);
        }

        base.Visit(node);
    }
}
