using EditorTest.Data;
using EditorTest.Extensions;
using EditorTest.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EditorTest.Classification;

internal class ClassifierSyntaxWalker(SnapshotSpan span, ClassificationRegistry registry) : SyntaxLocatorWalker
{
    private readonly ITextSnapshot _snapshot = span.Snapshot;
    private readonly List<ClassificationSpan> _results = new(capacity: 32);

    public IList<ClassificationSpan> Results => _results;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Add(int width, IClassificationType type, int offset = 0)
    {
        var s = new Span(Consumed + offset, width);
        if (!span.IntersectsWith(s))
        {
            return;
        }

        _results.Add(new(new(_snapshot, s), type));
    }

    public override void Visit(IdentifierToken node)
    {
        if (Peek() is FunctionCallExpressionSyntax && node.Value.Equals("var", StringComparison.OrdinalIgnoreCase))
        {
            Add(node.Width, registry.Keyword);
        }
        else if (Peek() is PseudoClassSelectorSyntax && node.Value.Equals("not", StringComparison.OrdinalIgnoreCase))
        {
            Add(node.Width, registry.Keyword);
        }
        else if (Peek() is PropertySyntax && CssWebData.Index.ValueKeywordsSet.Contains(node.Value))
        {
            Add(node.Width, registry.Keyword);
        }
        else
        {
            Add(node.Width, registry.Identifier);
        }
    }

    public override void Visit(ElementSelectorSyntax node)
    {
        base.VisitLeadingTrivia(node);
        Add(node.NameToken.Width, registry.MarkupElement);
        MarkAsConsumed(node.NameToken);
        base.VisitTrailingTrivia(node);
    }

    public override void Visit(ClassSelectorSyntax node)
    {
        base.VisitLeadingTrivia(node);
        base.VisitToken(node.DotToken);
        Add(node.NameToken.Width, registry.Type);
        MarkAsConsumed(node.NameToken);
        base.VisitTrailingTrivia(node);
    }

    public override void Visit(PropertySyntax node)
    {
        Add(node.NameSyntax.NameToken.Width, registry.PropertyName, node.LeadingTrivia.Width() + node.NameSyntax.LeadingTrivia.Width());

        var propertyName = node.NameSyntax.NameToken.Value;
        if (Peek() is RuleDeclarationSyntax rule)
        {
            var analyze = false;

            foreach (var item in rule.Nodes.Items.AsValueEnumerable())
            {
                // find current node
                if (object.ReferenceEquals(item, node))
                {
                    analyze = true;
                    continue;
                }

                // skip everything before
                if (!analyze)
                {
                    continue;
                }

                // find overwrite after
                if (item is PropertySyntax property)
                {
                    if (property.NameSyntax.NameToken.Value.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        Add(node.Width, registry.Unnecessary);
                        break;
                    }
                }
            }
        }

        base.Visit(node);
    }

    public override void Visit(WhiteSpaceTrivia node)
    {
        Add(node.Width, registry.WhiteSpace);
    }

    public override void Visit(PunctationToken node)
    {
        Add(node.Width, registry.Punctuation);
    }

    public override void Visit(NumberToken node)
    {
        Add(node.Width, registry.Number);
    }

    public override void Visit(StringToken node)
    {
        Add(node.Width, registry.String);
        Add(node.Width - 1 + (node.Value.EndsWith("\"") ? 1 : 0), registry.NaturalLanguage, offset: 1);
    }

    public override void Visit(SingleLineCommentTrivia node)
    {
        Add(node.Width, registry.Comment);
        Add(node.Width - 2, registry.NaturalLanguage, offset: 2);
    }

    public override void Visit(MultiLineCommentTrivia node)
    {
        Add(node.Width, registry.Comment);
        Add(node.Width - 2 + (node.Value.EndsWith("*/") ? 2 : 0), registry.NaturalLanguage, offset: 2);
    }

    public override void VisitToken(SyntaxToken token)
    {
        if (token.IsMissing())
        {
            return;
        }
        
        base.VisitToken(token);
    }

    public override void Visit(CombinatorSyntax node)
    {
        if (node.Items is [CompoundSelectorSyntax { Items: [ UniversalSelectorSyntax, ..] }, ..] or [UniversalSelectorSyntax, ..])
        {
            Add(1, registry.Unnecessary, node.LeadingTrivia.Width());
        }

        base.Visit(node);
    }

    public override void Visit(NumberWithUnitSyntax node)
    {
        if (node.NumberToken.Value == "0")
        {
            Add(node.UnitToken.Width, registry.Unnecessary, 1);
        }

        base.Visit(node);
    }
}
