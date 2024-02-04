using Css.Source;
using Css.Extensions;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Css.Syntax;

public abstract record class AbstractSyntaxNode()
{
    public abstract int Width { get; }

    public virtual void WriteTo(TextWriter writer) { }
}

public abstract record class SyntaxNode() : AbstractSyntaxNode()
{
    private IImmutableList<SyntaxTrivia> _leadingTrivia = ImmutableList<SyntaxTrivia>.Empty;
    public IImmutableList<SyntaxTrivia> LeadingTrivia
    {
        get => _leadingTrivia; 
        set
        {
            _leadingTrivia = value;
            CacheWidth();
        }
    }

    private IImmutableList<SyntaxTrivia> _trailingTrivia = ImmutableList<SyntaxTrivia>.Empty;
    public IImmutableList<SyntaxTrivia> TrailingTrivia
    {
        get => _trailingTrivia;
        set
        {
            _trailingTrivia = value;
            CacheWidth();
        }
    }

    public abstract int InnerWidth { get; }

    private int _width = -1;
    public override int Width
    {
        get
        {
            if (_width == -1)
            {
                CacheWidth();
            }

            return _width;
        }
    }

    private void CacheWidth() => _width = LeadingTrivia.Width() + InnerWidth + TrailingTrivia.Width();
}

public abstract record class RootSyntax() : SyntaxNode;

public record class DocumentSyntax(SyntaxList<SyntaxNode> Declarations) : RootSyntax
{
    public override int InnerWidth => Declarations.Width;
}

public record class InlineDocumentSyntax(SyntaxList<PropertySyntax> Properties) : RootSyntax
{
    public override int InnerWidth => Properties.Width;
}

public record class RuleDeclarationSyntax(SelectorListSyntax Selectors, SyntaxToken OpenBrace, SyntaxList<SyntaxNode> Nodes, SyntaxToken CloseBrace) : SyntaxNode
{
    public override int InnerWidth => Selectors.Width + OpenBrace.Width + Nodes.Width + CloseBrace.Width;
}

public abstract record class DirectiveSyntax(KeywordToken KeywordToken) : SyntaxNode
{

}

public abstract record class SimpleDirectiveSyntax(KeywordToken KeywordToken) : DirectiveSyntax(KeywordToken)
{

}

public abstract record class ComplexDirectiveSyntax(KeywordToken KeywordToken, PunctationToken OpenBrace, PunctationToken CloseBrace) : DirectiveSyntax(KeywordToken)
{

}

public record class ImportDirectiveSyntax(KeywordToken KeywordToken, WhiteSpaceTrivia Delimiter, StringToken PathToken, PunctationToken SemicolonToken) : SimpleDirectiveSyntax(KeywordToken)
{
	public override int InnerWidth => KeywordToken.Width + Delimiter.Width + PathToken.Width + SemicolonToken.Width;
}

public record class CharsetDirectiveSyntax(KeywordToken KeywordToken, WhiteSpaceTrivia Delimiter, StringToken CharsetToken, PunctationToken SemicolonToken) : SimpleDirectiveSyntax(KeywordToken)
{
    public override int InnerWidth => KeywordToken.Width + Delimiter.Width + CharsetToken.Width + SemicolonToken.Width;
}

public record class SpeculativeDirectiveSyntax(KeywordToken KeywordToken) : SimpleDirectiveSyntax(KeywordToken)
{
	public override int InnerWidth => KeywordToken.Width;
}

public record class FontFaceDirectiveSyntax(KeywordToken KeywordToken, PunctationToken OpenBrace, SyntaxList<PropertySyntax> Properties, PunctationToken CloseBrace) : ComplexDirectiveSyntax(KeywordToken, OpenBrace, CloseBrace)
{
	public override int InnerWidth => KeywordToken.Width + OpenBrace.Width + Properties.Width + CloseBrace.Width;
}



public abstract record class SimpleSelectorSyntax : SyntaxNode
{

}

public record class ElementSelectorSyntax(IdentifierToken NameToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => NameToken.Width;
}

public record class AttributeSelectorSyntax(
    PunctationToken OpenBracketToken, 
    IdentifierToken NameToken,
    OperatorToken? OperatorToken,
    StringToken? ValueToken,
    PunctationToken CloseBracketToken
) : SimpleSelectorSyntax
{
    public override int InnerWidth => OpenBracketToken.Width + NameToken.Width + (OperatorToken?.Width ?? 0) + (ValueToken?.Width ?? 0) + CloseBracketToken.Width;
}

public record class UniversalSelectorSyntax(PunctationToken StarToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => StarToken.Width;
}

public record class NestingSelectorSyntax(PunctationToken AmpersandToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => AmpersandToken.Width;
}

public record class PropertySyntax(IdentifierToken NameToken, PunctationToken ColonToken, PropertyValueSyntax ValueSyntax, PunctationToken SemicolonToken) : SyntaxNode
{
    public override int InnerWidth => NameToken.Width + ColonToken.Width + ValueSyntax.Width + SemicolonToken.Width;

    public bool IsVariable => NameToken.Value.StartsWith("--", StringComparison.Ordinal);

	public RelativeSourceSpan GetNameSpan() => new(this, LeadingTrivia.Width() + NameToken.LeadingTrivia.Width(), NameToken.InnerWidth);
}


public record class IdSelectorSyntax(PunctationToken HashToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => HashToken.Width + NameToken.Width;
}

public record class ClassSelectorSyntax(PunctationToken DotToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => DotToken.Width + NameToken.Width;
}

public record class PseudoClassSelectorSyntax(PunctationToken ColonToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => ColonToken.Width + NameToken.Width;
}

public record class PseudoElementSelectorSyntax(PunctationToken ColonsToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    public override int InnerWidth => ColonsToken.Width + NameToken.Width;
}




public record class SyntaxList<TSyntax>(IImmutableList<TSyntax> Items) : SyntaxNode() where TSyntax : AbstractSyntaxNode
{
    public override int InnerWidth => Items.Sum(i => i.Width);

    public override void WriteTo(TextWriter writer)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            item.WriteTo(writer);
        }
    }
}

public record class SeparatedSyntaxList<TSyntax>(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : SyntaxList<AbstractSyntaxNode>(NodesAndSeparators) where TSyntax : SyntaxNode
{
    public override int InnerWidth => Items.Sum(i => i.Width);

    public TSyntax this[int index] => (TSyntax)NodesAndSeparators[index / 2];

    public int Count => (Items.Count + 1) / 2;
}

public record class SpaceSeparatedSyntaxList<TSyntax>(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : SeparatedSyntaxList<TSyntax>(NodesAndSeparators) where TSyntax : SyntaxNode;

public record class SelectorListSyntax(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : CommaSeparatedSyntaxList<CombinatorSyntax>(NodesAndSeparators);

public record class CompoundSelectorSyntax(IImmutableList<SimpleSelectorSyntax> Selectors) : SyntaxList<SimpleSelectorSyntax>(Selectors)
{
    private bool _elementNameIsSet = false;
    private string? _elementName;

    public string? GetElementName()
    {
        if (_elementNameIsSet == false)
        {
            foreach (var selector in Selectors.AsValueEnumerable())
            {
                if (selector is ElementSelectorSyntax elementSelectorSyntax)
                {
                    _elementName = elementSelectorSyntax.NameToken.Value;
                    _elementNameIsSet = true;
                    break;
                }
            }
        }

        return _elementName;
    }
}

public record class CombinatorSyntax(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : SpaceSeparatedSyntaxList<CompoundSelectorSyntax>(NodesAndSeparators);

public record class PropertyValueSyntax(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : SpaceSeparatedSyntaxList<ExpressionSyntax>(NodesAndSeparators)
{
    public static readonly PropertyValueSyntax Empty = new(ImmutableList<AbstractSyntaxNode>.Empty);
}

public record class FunctionArgumentListSyntax(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : CommaSeparatedSyntaxList<ExpressionSyntax>(NodesAndSeparators)
{
    public static readonly FunctionArgumentListSyntax Empty = new(ImmutableList<AbstractSyntaxNode>.Empty);
}

public record class CommaSeparatedSyntaxList<TSyntax>(IImmutableList<AbstractSyntaxNode> NodesAndSeparators) : SeparatedSyntaxList<TSyntax>(NodesAndSeparators) where TSyntax : SyntaxNode;
