using System;

namespace Css.Syntax;

public class AnimationDefinitionFinder(Action<SnapshotNode<IdentifierToken>> found) : SyntaxNodeFinder<IdentifierToken>(found)
{
	public override void Visit(KeyframesDirectiveSyntax node)
	{
		if (node.IdentifierToken.Any())
		{
			Report(node.IdentifierToken);
		}

		base.Visit(node);
	}
}
