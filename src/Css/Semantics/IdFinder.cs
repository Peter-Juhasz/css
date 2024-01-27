using System;

namespace EditorTest.Syntax;

public class IdFinder(Action<string> found) : SyntaxLocatorWalker
{
    public override void Visit(IdSelectorSyntax node)
    {
        if (node.NameToken.Any())
        {
            found(node.NameToken.Value);
        }

        base.Visit(node);
    }

    public override void Visit(AttributeSelectorSyntax node)
    {
        if (node.NameToken.Value.Equals("id", StringComparison.OrdinalIgnoreCase) && node.OperatorToken?.Text == "=" && node.ValueToken?.Value.Length > 0)
        {
            found(node.ValueToken.Value);
        }

        base.Visit(node);
    }
}

