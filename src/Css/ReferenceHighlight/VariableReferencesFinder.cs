using System;

namespace Css.Syntax;

public class VariableReferencesFinder(string name, Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(IdentifierToken node)
    {
        if (node.Value == name)
        {
            Report(node);
        }

        base.Visit(node);
    }
}
