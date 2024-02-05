using Css.Data;
using System;

namespace Css.Syntax;

public class AnimationReferencesFinder : SyntaxNodeFinder<IdentifierToken>
{
	public AnimationReferencesFinder(string name, Action<SnapshotNode<IdentifierToken>> found) : base(found)
	{
		this.name = name;
	}

	private bool _searchValues = false;
	private readonly string name;


	public override void Visit(PropertySyntax node)
	{
		if (node.NameToken.Value.Equals("animation-name", StringComparison.OrdinalIgnoreCase))
		{
			_searchValues = true;
		}

		base.Visit(node);

		_searchValues = false;
	}

	public override void Visit(KeyframesDirectiveSyntax node)
	{
		if (name == node.IdentifierToken.Value)
		{
			Report(node.IdentifierToken, offset: node.GetNameSpan().RelativePosition);
		}

		base.Visit(node);
	}

	public override void Visit(IdentifierToken node)
	{
		if (_searchValues)
		{
			if (node.Value == name)
			{
				Report(node);
			}
		}

		base.Visit(node);
	}
}
