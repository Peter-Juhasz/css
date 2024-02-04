using System;

namespace Css.Syntax;

public class FunctionReferencesFinder(string name, Action<SnapshotNode<FunctionCallExpressionSyntax>> found) : SyntaxNodeFinder<FunctionCallExpressionSyntax>(found)
{
    public override void Visit(FunctionCallExpressionSyntax node)
    {
        if (node.NameToken.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            Report(node);
        }

        base.Visit(node);
    }
}
