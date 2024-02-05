using Css.Data;
using Css.Extensions;
using System;
using System.Linq;

namespace Css.Syntax;

public class PropertyReferencesFinder : SyntaxNodeFinder<IdentifierToken>
{
    public PropertyReferencesFinder(string name, Action<SnapshotNode<IdentifierToken>> found, bool matchVendorSpecifics = true) : base(found)
    {
        this.name = name;
        this.normalizedName = Normalize(name);
        this.matchVendorSpecifics = matchVendorSpecifics;
    }

    private bool _searchValues = false;
    private readonly bool matchVendorSpecifics;
    private readonly string normalizedName;
    private readonly string name;


	public override void Visit(PropertySyntax node)
	{
		if (Matches(node.NameToken.Value))
		{
			Report(node.NameToken, offset: node.LeadingTrivia.Width());
		}

		if (CssWebData.Index.Properties.TryGetValue(node.NameToken.Value, out var definition) &&
            definition.Restrictions != null && definition.Restrictions.Contains("property"))
        {
            _searchValues = true;
        }

        base.Visit(node);

        _searchValues = false;
    }

	public override void Visit(PropertyDirectiveSyntax node)
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
            if (Matches(node.Value))
            {
                Report(node);
            }
        }

        base.Visit(node);
    }

    private string Normalize(string name)
    {
        if (matchVendorSpecifics == false)
        {
            return name;
        }

        if (name.Length > 2 && name[0] == '-' && name[1] != '-')
        {
            foreach (var prefix in CssWebData.Index.VendorPrefixes.AsValueEnumerable())
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(prefix.Length);
                }
            }
        }

        return name;
    }

    private bool Matches(string name)
    {
        if (normalizedName.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (matchVendorSpecifics && name.Length >= 3 + normalizedName.Length && name[0] == '-' && name[1] != '-')
        {
            foreach (var prefix in CssWebData.Index.VendorPrefixes.AsValueEnumerable())
            {
                if (name.Length == normalizedName.Length + prefix.Length &&
                    name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(normalizedName, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return true;
                }
            }
        }

        return false;
    }
}
