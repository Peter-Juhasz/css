using Css.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Css.SmartIndent;

public class IndentationWalker(int position) : SyntaxLocatorWalker
{
    public int? Result { get; private set; }

    public int Depth { get; private set; } = 0;

    public override void Visit(RuleDeclarationSyntax node)
    {
        if (Consumed + node.Width < position)
        {
            MarkAsConsumed(node);
            return;
        }

        if (node.OpenBrace.Any())
        {
            //Result = GetIndentation(node.LeadingTrivia);

            if (Consumed + node.LeadingTrivia.Width() + node.Selectors.Width + node.OpenBrace.Width <= position &&
                position <= Consumed + node.Width - node.TrailingTrivia.Width() - node.CloseBrace.Width)
            {
                //Result += 4;
                Depth++;
                Result = Depth * 4;
            }
        }

        base.Visit(node);

        if (Consumed + node.Width > position)
        {
            Cancel();
            return;
        }
    }

    private int GetIndentation(IReadOnlyList<SyntaxTrivia> trivia)
    {
        var spaces = 0;

        for (int i = trivia.Count - 1; i >= 0; i--)
        {
            if (trivia[i] is not WhiteSpaceTrivia ws)
            {
                break;
            }

            var shallBreak = false;
            for (int j = ws.Width - 1; j >= 0; j--)
            {
                var ch = ws.Text[i];
                if (ch == '\t')
                {
                    spaces += 4;
                }
                else if (ch == ' ')
                {
                    spaces++;
                }
                else
                {
                    shallBreak = true;
                    break;
                }
            }

            if (shallBreak)
            {
                break;
            }
        }

        return spaces;
    }
}
