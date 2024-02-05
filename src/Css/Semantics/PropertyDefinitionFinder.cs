using System;

namespace Css.Syntax;

public class PropertyDefinitionFinder(Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
	public override void Visit(PropertyDirectiveSyntax node)
	{
        if (node.IdentifierToken.Any())
        {
            Report(node.IdentifierToken);
        }

		base.Visit(node);
	}
}
