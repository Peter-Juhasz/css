using Css.Syntax;
using System;

namespace Css.Outlining;

public class StructureSyntaxWalker(Action<SnapshotNode<RuleDeclarationSyntax>> found) : SyntaxNodeFinder<RuleDeclarationSyntax>(found)
{
    public override void Visit(RuleDeclarationSyntax node)
    {
        Report(node);
        base.Visit(node);
    }
}
