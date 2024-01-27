using EditorTest.Syntax;
using System;

namespace EditorTest.Outlining;

public class StructureSyntaxWalker(Action<SnapshotNode<RuleDeclarationSyntax>> found) : SyntaxNodeFinder<RuleDeclarationSyntax>(found)
{
    public override void Visit(RuleDeclarationSyntax node)
    {
        Report(node);
        base.Visit(node);
    }
}
