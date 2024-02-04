using Css.Source;
using Css.Syntax;
using System;

namespace Css.Formatting;

public class FormatWalker(Action<SourceChange> collect, int position = 0) : SyntaxLocatorWalker(position)
{
    public override void Visit(PropertySyntax node)
    {
        if (node.NameToken.LeadingTrivia.IsWhiteSpace())
        {
            collect(SourceChange.Delete(node.GetLeadingTriviaExtent().ToAbsolute(Consumed + node.LeadingTrivia.Width())));
        }

        if (node.NameToken.TrailingTrivia.IsWhiteSpace())
        {
            collect(SourceChange.Delete(node.GetTrailingTriviaExtent().ToAbsolute(Consumed + node.LeadingTrivia.Width())));
        }

        base.Visit(node);
    }

    public override void Visit(PropertyValueSyntax node)
    {
        if (node.LeadingTrivia is not [ WhiteSpaceTrivia { Text: " " }] && node.LeadingTrivia.HasComment())
        {
            collect(SourceChange.Replace(node.GetLeadingTriviaExtent().ToAbsolute(Consumed), " "));
        }

        if (node.TrailingTrivia.IsWhiteSpace())
        {
            collect(SourceChange.Delete(node.GetTrailingTriviaExtent().ToAbsolute(Consumed)));
        }

        base.Visit(node);
    }   
}
