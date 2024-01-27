using EditorTest.Data;
using System;

namespace EditorTest.Syntax;

public class ColorsFinder(Action<SnapshotNode<AbstractSyntaxNode>> found) : SyntaxNodeFinder<AbstractSyntaxNode>(found)
{
    public override void Visit(IdentifierToken node)
    {
        if (CssWebData.Index.NamedColors.ContainsKey(node.Value))
        {
            Report(node);
        }

        base.Visit(node);
    }

    public override void Visit(HexColorToken node)
    {
        Report(node);
        base.Visit(node);
    }
}
