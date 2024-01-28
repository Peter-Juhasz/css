using EditorTest.Extensions;

namespace EditorTest.Syntax;

public abstract class SyntaxWalker
{
    public virtual void Visit(SyntaxTree tree)
    {
        switch (tree.Root)
        {
            case InlineDocumentSyntax d: Visit(d); break;
            case DocumentSyntax d: Visit(d); break;
        }
    }

    public virtual void Visit(InlineDocumentSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.Properties.Items.AsValueEnumerable())
        {
            Visit(child);
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(DocumentSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.Declarations.Items.AsValueEnumerable())
        {
            switch (child)
            {
                case RuleDeclarationSyntax n: Visit(n); break;
                case DirectiveSyntax n: Visit(n); break;
            }
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(RuleDeclarationSyntax node)
    {
        VisitLeadingTrivia(node);
        Visit(node.Selectors);
        VisitToken(node.OpenBrace);
        foreach (var child in node.Nodes.Items.AsValueEnumerable())
        {
            switch (child)
            {
                case PropertySyntax n: Visit(n); break;
                case RuleDeclarationSyntax n: Visit(n); break;
            }
        }
        VisitToken(node.CloseBrace);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(SelectorListSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.NodesAndSeparators.AsValueEnumerable())
        {
            switch (child)
            {
                case CombinatorSyntax n: Visit(n); break;
                case CompoundSelectorSyntax n: Visit(n); break;
                case SimpleSelectorSyntax n: Visit(n); break;
                case PunctationToken n: VisitToken(n); break;
            }
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(CombinatorSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.NodesAndSeparators.AsValueEnumerable())
        {
            switch (child)
            {
                case CompoundSelectorSyntax n: Visit(n); break;
                case SimpleSelectorSyntax n: Visit(n); break;
                case PunctationToken n: VisitToken(n); break;
                case WhiteSpaceTrivia n: VisitTrivia(n); break;
            }
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(CompoundSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.Items.AsValueEnumerable())
        {
            Visit(child);
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(DirectiveSyntax node)
    {
        switch (node)
        {
            case ImportDirectiveSyntax n: Visit(n); break;
        }
    }

    public virtual void Visit(SimpleSelectorSyntax node)
    {
        switch (node)
        {
            case ElementSelectorSyntax n: Visit(n); break;
            case AttributeSelectorSyntax t: Visit(t); break;
            case UniversalSelectorSyntax n: Visit(n); break;
            case NestingSelectorSyntax n: Visit(n); break;
            case IdSelectorSyntax n: Visit(n); break;
            case ClassSelectorSyntax n: Visit(n); break;
            case PseudoClassSelectorSyntax n: Visit(n); break;
            case PseudoElementSelectorSyntax n: Visit(n); break;
        }
    }

    public virtual void Visit(ImportDirectiveSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.KeywordToken);
        VisitTrivia(node.Delimiter);
        VisitToken(node.PathToken);
        VisitToken(node.SemicolonToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(UniversalSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.StarToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(AttributeSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.OpenBracketToken);
        VisitToken(node.NameToken);
        if (node.OperatorToken != null)
        {
            VisitToken(node.OperatorToken);

            if (node.ValueToken != null)
            {
                VisitToken(node.ValueToken);
            }
        }
        VisitToken(node.CloseBracketToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(IdSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.HashToken);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(ClassSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.DotToken);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(PseudoClassSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.ColonToken);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(PseudoElementSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.ColonsToken);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(NestingSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.AmpersandToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(ElementSelectorSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(PropertySyntax node)
    {
        VisitLeadingTrivia(node);
        Visit(node.NameSyntax);
        VisitToken(node.ColonToken);
        Visit(node.ValueSyntax);
        VisitToken(node.SemicolonToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(PropertyNameSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(PropertyValueSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.NodesAndSeparators.AsValueEnumerable())
        {
            switch (child)
            {
                case ExpressionSyntax e: Visit(e); break;
                case WhiteSpaceTrivia t: VisitTrivia(t); break;
            }
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(FunctionCallExpressionSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.NameToken);
        VisitToken(node.OpenParenthesisToken);
        Visit(node.ArgumentListSyntax);
        VisitToken(node.CloseParenthesisToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(FunctionArgumentListSyntax node)
    {
        VisitLeadingTrivia(node);
        foreach (var child in node.NodesAndSeparators.AsValueEnumerable())
        {
            switch (child)
            {
                case ExpressionSyntax e: Visit(e); break;
                case PunctationToken t: VisitToken(t); break;
            }
        }
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(ExpressionSyntax node)
    {
        switch (node)
        {
            case NumberExpressionSyntax t: Visit(t); break;
            case NumberWithUnitSyntax t: Visit(t); break;
            case IdentifierExpressionSyntax t: Visit(t); break;
            case StringExpressionSyntax t: Visit(t); break;
            case FunctionCallExpressionSyntax t: Visit(t); break;
            case HexColorExpressionSyntax t: Visit(t); break;
        }
    }

    public virtual void Visit(HexColorExpressionSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.ColorToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(NumberExpressionSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.NumberToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(NumberWithUnitSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.NumberToken);
        VisitToken(node.UnitToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(IdentifierExpressionSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.NameToken);
        VisitTrailingTrivia(node);
    }

    public virtual void Visit(StringExpressionSyntax node)
    {
        VisitLeadingTrivia(node);
        VisitToken(node.Token);
        VisitTrailingTrivia(node);
    }

    public virtual void VisitLeadingTrivia(SyntaxNode node)
    {
        foreach (var trivia in node.LeadingTrivia.AsValueEnumerable())
        {
            VisitTrivia(trivia);
        }
    }

    public virtual void VisitTrailingTrivia(SyntaxNode node)
    {
        foreach (var trivia in node.TrailingTrivia.AsValueEnumerable())
        {
            VisitTrivia(trivia);
        }
    }


    public virtual void VisitToken(SyntaxToken trivia)
    {
        switch (trivia)
        {
            case NumberToken t: Visit(t); break;
            case PunctationToken t: Visit(t); break;
            case OperatorToken t: Visit(t); break;
            case StringToken t: Visit(t); break;
            case IdentifierToken t: Visit(t); break;
            case UnitToken t: Visit(t); break;
            case HexColorToken t: Visit(t); break;
            case KeywordToken t: Visit(t); break;
        }
    }

    public virtual void Visit(NumberToken node) { }

    public virtual void Visit(PunctationToken node) { }

    public virtual void Visit(OperatorToken node) { }

    public virtual void Visit(StringToken node) { }

    public virtual void Visit(IdentifierToken node) { }

    public virtual void Visit(UnitToken node) { }

    public virtual void Visit(HexColorToken node) { }

    public virtual void Visit(KeywordToken node) { }


    public virtual void VisitTrivia(SyntaxTrivia trivia)
    {
        switch (trivia)
        {
            case WhiteSpaceTrivia t: Visit(t); break;
            case SingleLineCommentTrivia t: Visit(t); break;
            case MultiLineCommentTrivia t: Visit(t); break;
        }
    }

    public virtual void Visit(WhiteSpaceTrivia node) { }

    public virtual void Visit(SingleLineCommentTrivia node) { }

    public virtual void Visit(MultiLineCommentTrivia node) { }
}
