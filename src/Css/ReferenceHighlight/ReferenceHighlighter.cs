using Css.Source;
using Css.Data;
using Css.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Css.ReferenceHighlight;

public class ReferenceHighlighter
{
    public IReadOnlyList<HighlightedSpan> GetHighlightedSpans(SyntaxTree syntaxTree, SourcePoint triggerPoint)
    {
        var matches = syntaxTree.FindNodeAt(triggerPoint.Position);
        return GetHighlightedSpans(syntaxTree, matches, triggerPoint);
    }

    public IReadOnlyList<HighlightedSpan> GetHighlightedSpans(SyntaxTree syntaxTree, SyntaxNodeSearchResult matches, SourcePoint triggerPoint)
    {
        var list = new List<HighlightedSpan>();
        Process(matches, n => InspectForPropertyName(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForVariablePropertyName(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForElementSelector(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForAttributeSelectorName(syntaxTree, n, triggerPoint, list));
        Process(matches, n => TryMatchAttributeSelectorValueQuotes(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForIdSelector(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForClassSelector(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForPseudoClassSelector(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForPseudoElementSelector(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForPropertyValue(syntaxTree, n, triggerPoint, list));
        Process(matches, n => InspectForNamedColor(syntaxTree, n, triggerPoint, list));
        TryMatchStringExpressionQuotes(syntaxTree, matches.Before, list);
        TryMatchStringExpressionQuotes(syntaxTree, matches.After, list);
        InspectForFunctionName(syntaxTree, matches.Contains, triggerPoint, list);
        InspectForFunctionName(syntaxTree, matches.After, triggerPoint, list);
        InspectForFunctionParenthesis(syntaxTree, matches.Contains, triggerPoint, list);
        InspectForFunctionParenthesis(syntaxTree, matches.Before, triggerPoint, list);
        InspectForDeclarationBraces(syntaxTree, matches.Contains, triggerPoint, list);
        InspectForDeclarationBraces(syntaxTree, matches.Before, triggerPoint, list);
        InspectForAttributeSelectorBrackets(syntaxTree, matches.After, triggerPoint, list);
        InspectForAttributeSelectorBrackets(syntaxTree, matches.Before, triggerPoint, list);
        return list ?? (IReadOnlyList<HighlightedSpan>)Array.Empty<HighlightedSpan>();
    }

    private bool Process(SyntaxNodeSearchResult matches, Action<SnapshotNode> tryComplete)
    {
        if (matches.After != null)
        {
            tryComplete(matches.After.Value);
        }

        if (matches.Contains != null)
        {
            tryComplete(matches.Contains.Value);
        }

        if (matches.Before != null)
        {
            tryComplete(matches.Before.Value);
        }

        return true;
    }


    private void InspectForPropertyName(SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Node is PropertySyntax propertyName)
		{
			if (!propertyName.GetNameSpan().ToAbsolute(node.Position).Contains(point))
			{
				return;
			}

			if (!CssWebData.Index.Properties.ContainsKey(propertyName.NameToken.Value))
            {
                return;
            }

            var finder = new PropertyReferencesFinder(propertyName.NameToken.Value, found =>
            {
                var span = new SourceSpan(found.Position, found.Node.InnerWidth);
                collect.Add(new(span, found.Parent is PropertyDirectiveSyntax ? "Definition" : "Reference"));
            });
            finder.Visit(syntaxTree);
        }
        else if (node.Node is IdentifierExpressionSyntax identifier)
        {
            if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var property))
            {
                return;
            }

            if (!CssWebData.Index.Properties.TryGetValue(property.NameToken.Value, out var definition))
            {
                return;
            }

            if (definition.Restrictions == null || !definition.Restrictions.Contains("property"))
            {
                return;
            }

            if (!CssWebData.Index.Properties.ContainsKey(identifier.NameToken.Value))
            {
                return;
            }

            var finder = new PropertyReferencesFinder(identifier.NameToken.Value, found =>
            {
                var span = new SourceSpan(found.Position, found.Node.InnerWidth);
                collect.Add(new(span));
            });
            finder.Visit(syntaxTree);
        }
    }

    private void InspectForVariablePropertyName(SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        string? variableName = null;
        if (node.Node is PropertySyntax propertyName && propertyName.NameToken.Value.StartsWith("--", StringComparison.Ordinal))
		{
			if (!propertyName.GetNameSpan().ToAbsolute(node.Position).Contains(point))
			{
				return;
			}

			variableName = propertyName.NameToken.Value;
        }
        else if (node.Node is IdentifierExpressionSyntax identifierSyntax && identifierSyntax.NameToken.Value.StartsWith("--", StringComparison.Ordinal) &&
            node.TryFindFirstAncestorUpwards<FunctionCallExpressionSyntax>(out var functionSyntax) && functionSyntax.NameToken.Value.Equals("var", StringComparison.Ordinal))
        {
            variableName = identifierSyntax.NameToken.Value;
        }

        if (variableName == null)
        {
            return;
        }

        var finder = new VariableReferencesFinder(variableName, found =>
        {
            var span = new SourceSpan(found.Position, found.Node.InnerWidth);
			collect.Add(new(span, found.Parent is PropertyDirectiveSyntax ? "Definition" : "Reference"));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForPropertyValue(SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Node is not IdentifierExpressionSyntax identifierSyntax)
        {
            return;
        }

        if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var propertySyntax))
        {
            return;
        }

        if (CssWebData.Index.Properties.TryGetValue(propertySyntax.NameToken.Value, out var definition))
        {
            // enums
            if (definition.Restrictions != null && definition.Restrictions.Contains("enum"))
            {
                if (definition.Values != null && definition.Values.Count != 0 && definition.Values.Any(v => v.Name == identifierSyntax.NameToken.Value))
                {
                    var finder = new PropertyValueReferencesFinder(propertySyntax.NameToken.Value, identifierSyntax.NameToken.Value, found =>
                    {
                        var span = new SourceSpan(found.Position, found.Node.InnerWidth);
                        collect.Add(new(span));
                    });
                    finder.Visit(syntaxTree);
				}
			}

            // animations
            if (definition.Name == "animation-name")
            {
                if (identifierSyntax.NameToken.Value != "none")
				{
					var finder = new AnimationReferencesFinder(identifierSyntax.NameToken.Value, found =>
					{
						var span = new SourceSpan(found.Position, found.Node.InnerWidth);
						collect.Add(new(span, found.Parent is KeyframesDirectiveSyntax ? "Definition" : "Reference"));
					});
					finder.Visit(syntaxTree);
				}
            }
		}
	}

    private void InspectForNamedColor(SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Node is not IdentifierExpressionSyntax identifierSyntax)
        {
            return;
        }

        if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var propertySyntax))
        {
            return;
        }

        if (!CssWebData.Index.Properties.TryGetValue(propertySyntax.NameToken.Value, out var definition))
        {
            return;
        }

        if (!definition.Restrictions.Contains("color"))
        {
            return;
        }

        if (!CssWebData.Index.NamedColors.TryGetValue(identifierSyntax.NameToken.Value, out var color))
        {
            return;
        }

        var finder = new ColorReferencesFinder(color, found => collect.Add(new(found)));
        finder.Visit(syntaxTree);
    }

    private void InspectForFunctionName(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not FunctionCallExpressionSyntax propertyName)
        {
            return;
        }

        if (!CssWebData.Index.FunctionNamesSet.Contains(propertyName.NameToken.Value))
        {
            return;
        }

        var nameSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width + 1);
        if (!nameSpan.Contains(point))
        {
            return;
        }

        var finder = new FunctionReferencesFinder(propertyName.NameToken.Value, found =>
        {
            var span = new SourceSpan(found.Position + found.Node.LeadingTrivia.Width(), found.Node.NameToken.Width);
            collect.Add(new(span));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForElementSelector(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not ElementSelectorSyntax propertyName)
        {
            return;
        }

        if (!HtmlWebData.Index.Elements.ContainsKey(propertyName.NameToken.Value))
        {
            return;
        }

        var nameSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width + 1);
        if (!nameSpan.Contains(point))
        {
            return;
        }

        var finder = new ElementReferencesFinder(propertyName.NameToken.Value, found =>
        {
            var span = new SourceSpan(found.Position, found.Node.Width);
            collect.Add(new(span));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForAttributeSelectorName(SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Node is not AttributeSelectorSyntax attributeSyntax)
        {
            return;
        }

        if (attributeSyntax.NameToken.IsMissing())
        {
            return;
        }

        if (!(node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width <= point.Position &&
            point.Position <= node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width + attributeSyntax.NameToken.Width
        ))
        {
            return;
        }

        if (!node.TryFindFirstAncestorUpwards<CompoundSelectorSyntax>(out var compoundSelectorSyntax) ||
            compoundSelectorSyntax.GetElementName() is not string elementName)
        {
            return;
        }

        if (!HtmlWebData.Index.GlobalAttributes.ContainsKey(attributeSyntax.NameToken.Value) &&
            (!HtmlWebData.Index.Elements.TryGetValue(elementName, out var element) || element.Attributes == null || !element.Attributes.Any(a => a.Name == attributeSyntax.NameToken.Value))
        )
        {
            return;
        }

        var finder = new AttributeReferencesFinder(elementName, attributeSyntax.NameToken.Value, found =>
        {
            var span = new SourceSpan(found.Position, found.Node.Width);
            collect.Add(new(span));
        });
        finder.Visit(syntaxTree);
    }

    private void TryMatchAttributeSelectorValueQuotes(SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Node is not AttributeSelectorSyntax attributeSyntax)
        {
            return;
        }

        if (attributeSyntax.ValueToken is not QuotedStringToken { IsClosed: true })
        {
            return;
        }

        int valueStartPosition = node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width + attributeSyntax.NameToken.Width + attributeSyntax.OperatorToken.Width;
        if (valueStartPosition != point.Position &&
            valueStartPosition + attributeSyntax.ValueToken.Width != point.Position
        )
        {
            return;
        }

        var openSpan = new SourceSpan(valueStartPosition, 1);
        collect.Add(new(openSpan, "Pair"));
        var closeSpan = new SourceSpan(valueStartPosition + attributeSyntax.ValueToken.Width - 1, 1);
        collect.Add(new(closeSpan, "Pair"));
    }

    private void InspectForIdSelector(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not IdSelectorSyntax propertyName)
        {
            return;
        }

        var nameSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.HashToken.Width, propertyName.NameToken.Width + 1);
        if (!nameSpan.Contains(point))
        {
            return;
        }

        var finder = new IdReferencesFinder(propertyName.NameToken.Value, found =>
        {
            collect.Add(new(found));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForPseudoClassSelector(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not PseudoClassSelectorSyntax propertyName)
        {
            return;
        }

        if (!CssWebData.Index.PseudoClasses.ContainsKey(propertyName.NameToken.Value))
        {
            return;
        }

        var nameSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.ColonToken.Width, propertyName.NameToken.Width + 1);
        if (!nameSpan.Contains(point))
        {
            return;
        }

        var finder = new PseudoClassReferencesFinder(propertyName.NameToken.Value, found =>
        {
            var span = new SourceSpan(found.Position, found.Node.Width);
            collect.Add(new(span));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForPseudoElementSelector(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not PseudoElementSelectorSyntax propertyName)
        {
            return;
        }

        if (!CssWebData.Index.PseudoElements.ContainsKey(propertyName.NameToken.Value))
        {
            return;
        }

        var nameSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.ColonsToken.Width, propertyName.NameToken.Width + 1);
        if (!nameSpan.Contains(point))
        {
            return;
        }

        var finder = new PseudoElementReferencesFinder(propertyName.NameToken.Value, found =>
        {
            var span = new SourceSpan(found.Position, found.Node.Width);
            collect.Add(new(span));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForClassSelector(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not ClassSelectorSyntax propertyName)
        {
            return;
        }

        var nameSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.DotToken.Width, propertyName.NameToken.Width + 1);
        if (!nameSpan.Contains(point))
        {
            return;
        }

        var finder = new ClassReferencesFinder(propertyName.NameToken.Value, found =>
        {
            collect.Add(new(found));
        });
        finder.Visit(syntaxTree);
    }

    private void InspectForFunctionParenthesis(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not FunctionCallExpressionSyntax propertyName)
        {
            return;
        }

        if (propertyName.OpenParenthesisToken.IsMissing() ||
            propertyName.CloseParenthesisToken.IsMissing())
        {
            return;
        }

        if (point.Position == node.Value.Position + propertyName.Width - propertyName.TrailingTrivia.Width() ||
            point.Position == node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.NameToken.Width
        )
        {
            var openSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.NameToken.Width, 1);
            collect.Add(new(openSpan, "Pair"));

            var closeSpan = new SourceSpan(node.Value.Position + propertyName.Width - propertyName.TrailingTrivia.Width() - 1, 1);
            collect.Add(new(closeSpan, "Pair"));
        }
    }

    private void InspectForDeclarationBraces(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not RuleDeclarationSyntax ruleSyntax)
        {
            return;
        }

        if (ruleSyntax.OpenBrace.IsMissing() ||
            ruleSyntax.CloseBrace.IsMissing())
        {
            return;
        }

        if (point.Position == node.Value.Position + ruleSyntax.Width - ruleSyntax.TrailingTrivia.Width() ||
            point.Position == node.Value.Position + ruleSyntax.LeadingTrivia.Width() + ruleSyntax.Selectors.Width
        )
        {
            var openSpan = new SourceSpan(node.Value.Position + ruleSyntax.LeadingTrivia.Width() + ruleSyntax.Selectors.Width, 1);
            collect.Add(new(openSpan, "Pair"));

            var closeSpan = new SourceSpan(node.Value.Position + ruleSyntax.Width - ruleSyntax.TrailingTrivia.Width() - 1, 1);
            collect.Add(new(closeSpan, "Pair"));
        }
    }

    private void TryMatchStringExpressionQuotes(SyntaxTree syntaxTree, SnapshotNode? node, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not StringExpressionSyntax propertyName)
        {
            return;
        }

        if (propertyName.Token is not QuotedStringToken quoted || !quoted.IsClosed)
        {
            return;
        }

        var startSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width(), 1);
        collect.Add(new(startSpan, "Pair"));

        var endSpan = new SourceSpan(node.Value.Position + propertyName.LeadingTrivia.Width() + propertyName.Token.Width - 1, 1);
        collect.Add(new(endSpan, "Pair"));
    }

    private void InspectForAttributeSelectorBrackets(SyntaxTree syntaxTree, SnapshotNode? node, SourcePoint point, IList<HighlightedSpan> collect)
    {
        if (node == null)
        {
            return;
        }

        if (node.Value.Node is not AttributeSelectorSyntax selectorSyntax)
        {
            return;
        }

        if (selectorSyntax.CloseBracketToken.IsMissing())
        {
            return;
        }

        if (point.Position != node.Value.Position + selectorSyntax.LeadingTrivia.Width() &&
            point.Position != node.Value.Position + selectorSyntax.Width - selectorSyntax.TrailingTrivia.Width()
        )
        {
            return;
        }

        var startSpan = new SourceSpan(node.Value.Position + selectorSyntax.LeadingTrivia.Width(), 1);
        collect.Add(new(startSpan, "Pair"));

        var endSpan = new SourceSpan(node.Value.Position + selectorSyntax.Width - selectorSyntax.TrailingTrivia.Width() - selectorSyntax.CloseBracketToken.Width, 1);
        collect.Add(new(endSpan, "Pair"));
    }
}
