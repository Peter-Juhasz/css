using System;
using System.Xml.Linq;

namespace Css.Syntax;

public class ClassesFinder(Action<string> found) : SyntaxLocatorWalker
{
    public override void Visit(ClassSelectorSyntax node)
    {
        if (node.NameToken.Any())
        {
            found(node.NameToken.Value);
        }

        base.Visit(node);
    }

    public override void Visit(AttributeSelectorSyntax node)
    {
        if (node.NameToken.Value.Equals("class", StringComparison.OrdinalIgnoreCase) && node.OperatorToken?.Text == "~=" && node.ValueToken?.Value.Length > 0)
        {
            found(node.ValueToken.Value);
        }

        base.Visit(node);
    }
}

