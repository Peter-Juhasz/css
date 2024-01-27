using EditorTest.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace EditorTest.SmartIndent;

[Export(typeof(ISmartIndentProvider))]
[ContentType(EditorClassifier1.ContentType)]
internal class CssSmartIndentationProvider : ISmartIndentProvider
{
    public ISmartIndent CreateSmartIndent(ITextView textView) => new SmartIndent();

    private class SmartIndent : ISmartIndent
    {
        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            var syntaxTree = line.Snapshot.GetSyntaxTree();
            var walker = new IndentationWalker(line.Start.Position);
            walker.Visit(syntaxTree);
            return walker.Result;
        }

        public void Dispose() { }
    }
}

internal class IndentationWalker(int position) : SyntaxLocatorWalker
{
    public int? Result { get; private set; }

    public override void Visit(RuleDeclarationSyntax node)
    {
        if (Consumed + node.Width < position)
        {
            MarkAsConsumed(node);
            return;
        }

        if (node.OpenBrace.Any())
        {
            Result = GetIndentation(node.LeadingTrivia);

            if (Consumed + node.LeadingTrivia.Width() + node.Selectors.Width + node.OpenBrace.Width < position &&
                position < Consumed + node.Width - node.TrailingTrivia.Width() - node.CloseBrace.Width)
            {
                Result += 4;
            }
        }

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
