using System;

namespace EditorTest.Syntax;

public class VariableDefinitionFinder(Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
    public override void Visit(IdentifierToken node)
    {
        if (node.Value.StartsWith("--", StringComparison.Ordinal))
        {
            Report(node);
        }

        base.Visit(node);
    }
}
