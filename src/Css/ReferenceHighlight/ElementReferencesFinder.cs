using System;

namespace EditorTest.Syntax;

public class ElementReferencesFinder(string name, Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(ElementSelectorSyntax node)
    {
        if (node.NameToken.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            Report(node.NameToken, offset: node.LeadingTrivia.Width());
        }

        base.Visit(node);
    }
}
