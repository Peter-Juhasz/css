using System;

namespace EditorTest.Syntax;

public class PropertyValueReferencesFinder(string propertyName, string valueName, Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(PropertySyntax node)
    {
        if (node.NameSyntax.NameToken.Value != propertyName)
        {
            MarkAsConsumed(node);
            return;
        }

        base.Visit(node);
    }

    public override void Visit(IdentifierToken node)
    {
        if (Peek() is PropertySyntax)
        {
            if (node.Value == valueName)
            {
                Report(node);
            }
        }

        base.Visit(node);
    }
}
