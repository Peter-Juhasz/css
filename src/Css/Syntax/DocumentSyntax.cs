using EditorTest.Extensions;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace EditorTest.Syntax;

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

    protected abstract int ChildrenWidth { get; }

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

    private void CacheWidth() => _width = LeadingTrivia.Width() + ChildrenWidth + TrailingTrivia.Width();
}

public abstract record class RootSyntax() : SyntaxNode;

public record class DocumentSyntax(SyntaxList<SyntaxNode> Declarations) : RootSyntax
{
    protected override int ChildrenWidth => Declarations.Width;
}

public record class InlineDocumentSyntax(SyntaxList<PropertySyntax> Properties) : RootSyntax
{
    protected override int ChildrenWidth => Properties.Width;
}

public record class RuleDeclarationSyntax(SelectorListSyntax Selectors, SyntaxToken OpenBrace, SyntaxList<SyntaxNode> Nodes, SyntaxToken CloseBrace) : SyntaxNode
{
    protected override int ChildrenWidth => Selectors.Width + OpenBrace.Width + Nodes.Width + CloseBrace.Width;
}

public abstract record class SimpleSelectorSyntax : SyntaxNode
{

}

public record class ElementSelectorSyntax(IdentifierToken NameToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => NameToken.Width;
}

public record class AttributeSelectorSyntax(
    PunctationToken OpenBracketToken, 
    IdentifierToken NameToken,
    OperatorToken? OperatorToken,
    StringToken? ValueToken,
    PunctationToken CloseBracketToken
) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => OpenBracketToken.Width + NameToken.Width + (OperatorToken?.Width ?? 0) + (ValueToken?.Width ?? 0) + CloseBracketToken.Width;
}

public record class UniversalSelectorSyntax(PunctationToken StarToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => StarToken.Width;
}

public record class NestingSelectorSyntax(PunctationToken AmpersandToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => AmpersandToken.Width;
}

public record class PropertySyntax(PropertyNameSyntax NameSyntax, PunctationToken ColonToken, PropertyValueSyntax ValueSyntax, PunctationToken SemicolonToken) : SyntaxNode
{
    protected override int ChildrenWidth => NameSyntax.Width + ColonToken.Width + ValueSyntax.Width + SemicolonToken.Width;
}

public record class PropertyNameSyntax(IdentifierToken NameToken) : SyntaxNode
{
    protected override int ChildrenWidth => NameToken.Width;
}

public abstract record class ExpressionSyntax : SyntaxNode
{

}

public record class NumberExpressionSyntax(NumberToken NumberToken) : ExpressionSyntax
{
    protected override int ChildrenWidth => NumberToken.Width;
}

public record class NumberWithUnitSyntax(NumberToken NumberToken, UnitToken UnitToken) : ExpressionSyntax
{
    protected override int ChildrenWidth => NumberToken.Width + UnitToken.Width;
}

public record class IdentifierExpressionSyntax(IdentifierToken NameToken) : ExpressionSyntax
{
    protected override int ChildrenWidth => NameToken.Width;
}

public record class HexColorExpressionSyntax(HexColorToken ColorToken) : ExpressionSyntax
{
    protected override int ChildrenWidth => ColorToken.Width;
}

public record class StringExpressionSyntax(StringToken Token) : ExpressionSyntax
{
    protected override int ChildrenWidth => Token.Width;
}

public record class FunctionCallExpressionSyntax(IdentifierToken NameToken, PunctationToken OpenParenthesisToken, FunctionArgumentListSyntax ArgumentListSyntax, PunctationToken CloseParenthesisToken) : ExpressionSyntax
{
    protected override int ChildrenWidth => NameToken.Width + OpenParenthesisToken.Width + ArgumentListSyntax.Width + CloseParenthesisToken.Width;
}


public record class SyntaxToken(string Text) : AbstractSyntaxNode
{
    public override int Width => Text.Length;

    public override void WriteTo(TextWriter writer) => writer.Write(Text);
}

public record class IdentifierToken(string Text) : SyntaxToken(Text)
{
    public virtual string Value => Text;
}

public abstract record class LiteralToken(string Text) : SyntaxToken(Text)
{
    public abstract string Value { get; }
}

public record class KeywordToken(string Name) : SyntaxToken(Name);

public abstract record class StringToken(string Text) : LiteralToken(Text);

public record class ImplicitStringToken(string Text) : StringToken(Text)
{
    public override string Value => Text;
}

public record class QuotedStringToken(string Text) : StringToken(Text)
{
    public override string Value => IsClosed ? Text[1..^1] : Text[1..];

    public char Quote => Text[0];

    public bool IsClosed => Text.Length > 1 && Text[^1] == Quote;
}

public record class PunctationToken(string Text) : SyntaxToken(Text);

public record class OperatorToken(string Text) : SyntaxToken(Text);

public record class NumberToken(string Text) : LiteralToken(Text)
{
    public override string Value => Text;
}

public record class HexColorToken(string Text) : LiteralToken(Text)
{
    public override string Value => Text;
}

public record class UnitToken(string Text) : SyntaxToken(Text);

public record class IdSelectorSyntax(PunctationToken HashToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => HashToken.Width + NameToken.Width;
}

public record class ClassSelectorSyntax(PunctationToken DotToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => DotToken.Width + NameToken.Width;
}

public record class PseudoClassSelectorSyntax(PunctationToken ColonToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => ColonToken.Width + NameToken.Width;
}

public record class PseudoElementSelectorSyntax(PunctationToken ColonsToken, IdentifierToken NameToken) : SimpleSelectorSyntax
{
    protected override int ChildrenWidth => ColonsToken.Width + NameToken.Width;
}


public record class SyntaxTrivia(string Text) : AbstractSyntaxNode
{
    public override int Width => Text.Length;

    public override void WriteTo(TextWriter writer) => writer.Write(Text);
}

public record class WhiteSpaceTrivia(string Text) : SyntaxTrivia(Text);

public abstract record class CommentTrivia(string Text) : SyntaxTrivia(Text)
{
    public abstract string Value { get; }
}

public record class SingleLineCommentTrivia(string Text) : CommentTrivia(Text)
{
    public override string Value => Text[2..];
}

public record class MultiLineCommentTrivia(string Text) : CommentTrivia(Text)
{
    public override string Value => Text.EndsWith("*/", StringComparison.Ordinal) ? Text[2..^2] : Text[2..];

    public bool IsClosed => Text.EndsWith("*/", StringComparison.Ordinal);
}


public record class SyntaxList<TSyntax>(IImmutableList<TSyntax> Items) : SyntaxNode() where TSyntax : AbstractSyntaxNode
{
    protected override int ChildrenWidth => Items.Sum(i => i.Width);

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
    protected override int ChildrenWidth => Items.Sum(i => i.Width);

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
