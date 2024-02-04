using Css.Data;
using Css.Extensions;
using Css.Syntax;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Css.Source;

namespace Css.Hosting.VisualStudio.CodeCompletion;

[Export(typeof(ICompletionSourceProvider))]
[ContentType(EditorClassifier1.ContentType)]
[Name("RobotsTxtCompletion")]
internal sealed class CssCompletionSourceProvider : ICompletionSourceProvider
{
#pragma warning disable 649

    [Import]
    private IGlyphService GlyphService;

#pragma warning restore 649


    public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer) => new CompletionSource(textBuffer, GlyphService);


    private sealed class CompletionSource(ITextBuffer buffer, IGlyphService glyphService) : ICompletionSource
    {
        private readonly ITextBuffer _buffer = buffer;
        private readonly ImageSource _propertyGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _elementGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphXmlItem, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _element2Glyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphXmlItem, StandardGlyphItem.GlyphItemPrivate);
        private readonly ImageSource _attributeGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphXmlAttribute, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _variableGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _keywordGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _functionGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupMethod, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _enumGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupEnumMember, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _constantGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupConstant, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _classGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic);
        private readonly ImageSource _eventGlyph = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupEvent, StandardGlyphItem.GlyphItemPublic);
        private bool _disposed = false;

        private IReadOnlyList<Completion>? _properties;
        private IReadOnlyList<Completion>? _elements;
        private IReadOnlyList<Completion>? _globalAttributes;
        private IReadOnlyList<Completion>? _units;
        private IReadOnlyList<Completion>? _valueKeywords;
        private IReadOnlyList<Completion>? _directiveKeywords;
        private IReadOnlyList<Completion>? _functions;
        private IReadOnlyList<Completion>? _colors;
        private IReadOnlyList<Completion>? _systemColors;
        private IReadOnlyList<Completion>? _allColors;
        private IReadOnlyList<Completion>? _pseudoClasses;
        private IReadOnlyList<Completion>? _pseudoElements;
        private Dictionary<string, IReadOnlyList<Completion>> _propertyValues = new();
        private Dictionary<string, IReadOnlyList<Completion>> _elementAttributes = new();


        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                return;

            // get snapshot
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(snapshot);
            if (triggerPoint == null)
                return;

            // get or compute syntax tree
            SyntaxTree syntaxTree = snapshot.GetSyntaxTree();
            var matches = syntaxTree.FindNodeAt(triggerPoint.Value.Position);

            var sourceTriggerPoint = triggerPoint.Value.ToSourcePoint();
            var builder = new CssCompletionsBuilder();
            Process(matches, m => TryCompletePropertyName(snapshot, syntaxTree, m, sourceTriggerPoint, builder));
            Process(matches, m => TryCompleteElementSelector(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryAddKeywords(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryCompleteUnits(snapshot, syntaxTree, m, triggerPoint.Value, builder));
            Process(matches, m => TryAddFunctions(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryAddPropertyValues(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryAddColors(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryCompleteVarArgument(snapshot, syntaxTree, m, triggerPoint.Value, builder));
            Process(matches, m => TryCompleteClassSelector(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryCompleteIdSelector(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryCompletePseudoClassSelector(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryCompletePseudoElementSelector(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryAddPropertiesAsValues(snapshot, syntaxTree, m, builder));
            Process(matches, m => TryCompleteAttributeSelectorName(snapshot, syntaxTree, m, triggerPoint.Value, builder));
            Process(matches, m => TryCompleteAttributeSelectorValue(snapshot, syntaxTree, m, triggerPoint.Value, builder));
            Process(matches, m => TryCompleteDirectiveKeywords(snapshot, syntaxTree, m, builder));

            if (builder.Completions.Count == 0)
                return;

            builder.Sort();
            var allTogether = new CssCompletionSet(builder);
            completionSets.Add(allTogether);
        }


        private bool Process(SyntaxNodeSearchResult matches, Func<SnapshotNode, bool> tryComplete)
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

        private bool TryCompletePropertyName(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, SourcePoint point, CssCompletionsBuilder builder)
        {
            if (node.Node is not PropertySyntax syntax)
            {
                return false;
            }

            if (!syntax.GetNameSpan().ToAbsolute(node.Position).Contains(point))
            {
                return false;
            }

            // basic properties
            if (_properties == null)
            {
                _properties = CssWebData.Index.PropertiesSorted.Select(ToCompletion).ToList();
            }
            var effective = _properties;

            // font-face
            if (node.TryFindFirstAncestorUpwards<FontFaceDirectiveSyntax>(out _))
			{
				effective = CssWebData.Index.FontFacePropertiesSorted.Select(ToCompletion).ToList();
			}

            // color-profile
            else if (node.TryFindFirstAncestorUpwards<ColorProfileDirectiveSyntax>(out _))
			{
				effective = CssWebData.Index.ColorProfilePropertiesSorted.Select(ToCompletion).ToList();
			}

			// property
			else if (node.TryFindFirstAncestorUpwards<PropertyDirectiveSyntax>(out _))
			{
				effective = CssWebData.Index.PropertyPropertiesSorted.Select(ToCompletion).ToList();
			}

			var span = syntax.GetNameSpan().ToAbsolute(node.Position).ToSpan();
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.Property, "Properties", "P", "Properties"));
            builder.Add(effective);

            // in regular context
			if (node.TryFindFirstAncestorUpwards<RuleDeclarationSyntax>(out _))
            {
			    // add variables
				var variables = new Dictionary<string, Completion>();
				var finder = new VariableDefinitionFinder(n =>
				{
					if (n.Position == node.Position)
					{
						return;
					}

					var nameToken = (IdentifierToken)n.Node;
					var name = nameToken.Value;
					if (variables.ContainsKey(name))
					{
						return;
					}

					var completion = new Completion(name, name, null, _variableGlyph, null);
					variables.Add(name, completion);
				});
				finder.Visit(syntaxTree);

				builder.EnableVariableCompletionBuilder = true;
				builder.AddFilter(new IntellisenseFilter(KnownMonikers.LocalVariable, "Variables", "V", "Variables"));
				builder.Add(variables.Values);

                // add html elements
                if (syntax.ColonToken.IsMissing())
                {
                    if (_elements == null)
                    {
						_elements = HtmlWebData.Index.ElementsSorted.Select(ToCompletion).ToList();
                    }

                    builder.AddFilter(new IntellisenseFilter(KnownMonikers.XMLElement, "Tags", "T", "Tags"));
                    builder.Add(_elements);
                }
			}

			return true;
        }

        private bool TryAddPropertiesAsValues(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not IdentifierExpressionSyntax propertyName)
            {
                return false;
            }

            if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var propertySyntax) ||
                !CssWebData.Index.Properties.TryGetValue(propertySyntax.NameToken.Value, out var definition) ||
                definition.Restrictions == null || !definition.Restrictions.Contains("property"))
            {
                return false;
            }

            if (_properties == null)
            {
                _properties = CssWebData.Index.PropertiesSorted.Select(p => ToCompletion(p)).ToList();
            }

            var span = new Span(node.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.Property, "Properties", "P", "Properties"));
            builder.Add(_properties);
            return true;
        }

        private bool TryCompleteElementSelector(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is ElementSelectorSyntax elementSelectorSyntax)
            {
                if (_elements == null)
                {
                    _elements = HtmlWebData.Index.ElementsSorted.Select(p => ToCompletion(p)).ToList();
                }

                var span = new Span(node.Position + elementSelectorSyntax.LeadingTrivia.Width(), elementSelectorSyntax.NameToken.Width);
                var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
                builder.SetSpan(trackingSpan);
                builder.AddFilter(new IntellisenseFilter(KnownMonikers.XMLElement, "Tags", "T", "Tags"));
                builder.Add(_elements);
                return true;
            }

            return false;
        }

        private bool TryCompleteAttributeSelectorName(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, SnapshotPoint point, CssCompletionsBuilder builder)
        {
            if (node.Node is not AttributeSelectorSyntax attributeSyntax)
            {
                return false;
            }

            if (_globalAttributes == null)
            {
                _globalAttributes = HtmlWebData.Index.GlobalAttributesSorted.Select(p => ToCompletion(p)).ToList();
            }

            if (!(node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width <= point.Position &&
                node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width + attributeSyntax.NameToken.Width >= point.Position))
            {
                return false;
            }

            var effective = _globalAttributes;
            if (node.TryFindFirstAncestorUpwards<CompoundSelectorSyntax>(out var compoundSelectorSyntax))
            {
                if (compoundSelectorSyntax.GetElementName() is string elementName)
                {
                    if (HtmlWebData.Index.Elements.TryGetValue(elementName, out var definition))
                    {
                        if (!_elementAttributes.TryGetValue(definition.Name, out effective))
                        {
                            effective = _globalAttributes.Concat(definition.Attributes.Select(ToCompletion)).ToList();
                            _elementAttributes.Add(definition.Name, effective);
                        }
                    }
                }
            }

            var span = new Span(node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width, attributeSyntax.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.XMLAttribute, "Attributes", "A", "Attributes"));
            builder.Add(effective);
            return true;
        }

        private bool TryCompleteAttributeSelectorValue(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, SnapshotPoint point, CssCompletionsBuilder builder)
        {
            if (node.Node is not AttributeSelectorSyntax attributeSyntax)
            {
                return false;
            }

            if (attributeSyntax.OperatorToken == null)
            {
                return false;
            }

            if (!(node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width + attributeSyntax.NameToken.Width + attributeSyntax.OperatorToken.Width <= point.Position &&
                node.Position + attributeSyntax.LeadingTrivia.Width() + attributeSyntax.OpenBracketToken.Width + attributeSyntax.NameToken.Width + attributeSyntax.OperatorToken.Width + (attributeSyntax.ValueToken?.Width ?? 0) >= point.Position))
            {
                return false;
            }

            var attributeName = attributeSyntax.NameToken.Value;

            HtmlAttributeData? effective = null;
            if (node.TryFindFirstAncestorUpwards<CompoundSelectorSyntax>(out var compoundSelectorSyntax))
            {
                if (compoundSelectorSyntax.GetElementName() is string elementName)
                {
                    if (HtmlWebData.Index.Elements.TryGetValue(elementName, out var element))
                    {
                        if (element.Attributes.FirstOrDefault(a => a.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase)) is HtmlAttributeData elementAttribute)
                        {
                            effective = elementAttribute;
                        }
                    }
                }
            }

            if (effective == null)
            {
                if (HtmlWebData.Index.GlobalAttributes.TryGetValue(attributeName, out var global))
                {
                    effective = global;
                }
            }

            if (effective == null)
            {
                return false;
            }

            var span = new Span(node.Position + 
                attributeSyntax.LeadingTrivia.Width() + 
                attributeSyntax.OpenBracketToken.Width +
                attributeSyntax.NameToken.Width + 
                attributeSyntax.OperatorToken.Width +
                (attributeSyntax.ValueToken is ImplicitStringToken ? 0 : 1),
                attributeSyntax.ValueToken.Width -
                (attributeSyntax.ValueToken is QuotedStringToken quoted ? (quoted.IsClosed ? 2 : 1) : 0)
            );
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);

            if (attributeSyntax.NameToken.Value.Equals("id", StringComparison.OrdinalIgnoreCase) && attributeSyntax.OperatorToken?.Text == "=")
            {
                var classes = new Dictionary<string, Completion>();
                var finder = new IdFinder(token =>
                {
                    classes[token] = new Completion(token, token, null, _variableGlyph, null);
                });
                finder.Visit(syntaxTree);

                builder.EnableIdCompletionBuilder = true;
                builder.AddFilter(new IntellisenseFilter(KnownMonikers.LocalVariable, "Element Ids", "I", "Ids"));
                builder.Add(classes.Values);
            }
            else if (attributeSyntax.NameToken.Value.Equals("class", StringComparison.OrdinalIgnoreCase) && attributeSyntax.OperatorToken?.Text == "~=")
            {
                var classes = new Dictionary<string, Completion>();
                var finder = new ClassesFinder(token =>
                {
                    classes[token] = new Completion(token, token, null, _classGlyph, null);
                });
                finder.Visit(syntaxTree);

                builder.EnableClassCompletionBuilder = true;
                builder.AddFilter(new IntellisenseFilter(KnownMonikers.Class, "Classes", "C", "Classes"));
                builder.Add(classes.Values);
            }
            else if (effective.ValueSet != null && HtmlWebData.Index.ValueSets.TryGetValue(effective.ValueSet, out var valueSet))
            {
                var completions = valueSet.Values.Select(v => new Completion(v.Name, v.Name, null, _enumGlyph, null)).ToList();

                builder.AddFilter(new IntellisenseFilter(KnownMonikers.Enumeration, "Values", "V", "Values"));
                builder.Add(completions);
            }

            return true;
        }

        private bool TryCompleteClassSelector(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not ClassSelectorSyntax selectorSyntax)
            {
                return false;
            }

            var classes = new Dictionary<string, Completion>();
            var finder = new ClassesFinder(token =>
            {
                classes[token] = new Completion(token, token, null, _classGlyph, null);
            });
            finder.Visit(syntaxTree);

            builder.EnableClassCompletionBuilder = true;
            var span = new Span(node.Position + selectorSyntax.LeadingTrivia.Width() + selectorSyntax.DotToken.Width, selectorSyntax.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.Class, "Classes", "C", "Classes"));
            builder.Add(classes.Values);
            return true;
        }

        private bool TryCompleteIdSelector(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not IdSelectorSyntax selectorSyntax)
            {
                return false;
            }

            var classes = new Dictionary<string, Completion>();
            var finder = new IdFinder(token =>
            {
                classes[token] = new Completion(token, token, null, _variableGlyph, null);
            });
            finder.Visit(syntaxTree);

            builder.EnableIdCompletionBuilder = true;
            var span = new Span(node.Position + selectorSyntax.LeadingTrivia.Width() + selectorSyntax.HashToken.Width, selectorSyntax.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.LocalVariable, "Element Ids", "I", "Ids"));
            builder.Add(classes.Values);
            return true;
        }

        private bool TryCompletePseudoClassSelector(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not PseudoClassSelectorSyntax elementSelectorSyntax)
            {
                return false;
            }

            if (_pseudoClasses == null)
            {
                _pseudoClasses = CssWebData.Index.PseudoClassesSorted.Select(p => ToCompletion(p)).ToList();
            }

            var span = new Span(node.Position + elementSelectorSyntax.LeadingTrivia.Width() + elementSelectorSyntax.ColonToken.Width, elementSelectorSyntax.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.Class, "Pseudo-classes", "P", "Pseudo-classes"));
            builder.Add(_pseudoClasses);
            return true;
        }

        private bool TryCompletePseudoElementSelector(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not PseudoElementSelectorSyntax elementSelectorSyntax)
            {
                return false;
            }

            if (_pseudoElements == null)
            {
                _pseudoElements = CssWebData.Index.PseudoElementsSorted.Select(p => ToCompletion(p)).ToList();
            }

            var span = new Span(node.Position + elementSelectorSyntax.LeadingTrivia.Width() + elementSelectorSyntax.ColonsToken.Width, elementSelectorSyntax.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.XMLElement, "Pseudo-elements", "P", "Pseudo-elements"));
            builder.Add(_pseudoElements);
            return true;
        }

        private bool TryAddKeywords(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not IdentifierExpressionSyntax propertyName)
            {
                return false;
            }

            if (node.TryFindFirstAncestorUpwards<FunctionCallExpressionSyntax>(out var functionCallExpressionSyntax))
            {
                if (functionCallExpressionSyntax.NameToken.Value.Equals("var", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (_valueKeywords == null)
            {
                _valueKeywords = CssWebData.Index.ValueKeywordsSorted.Select(p => new Completion(p, p, null, _keywordGlyph, null)).ToList();
            }

            var span = new Span(node.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.KeywordSnippet, "Keywords", "K", "Keywords"));
            builder.Add(_valueKeywords);
            return true;
        }

        private bool TryCompleteDirectiveKeywords(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not SimpleDirectiveSyntax directiveSyntax)
            {
                return false;
            }

            if (_directiveKeywords == null)
            {
                _directiveKeywords = CssWebData.Index.DirectivesSorted.Select(p => new Completion(p.Name, p.Name, p.Description, _keywordGlyph, null)).ToList();
            }

            var span = new Span(node.Position + directiveSyntax.LeadingTrivia.Width(), directiveSyntax.KeywordToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.KeywordSnippet, "Keywords", "K", "Keywords"));
            builder.Add(_directiveKeywords);
            return true;
        }

        private bool TryAddFunctions(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not IdentifierExpressionSyntax propertyName)
            {
                return false;
            }

            if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var propertySyntax))
            {
                return false;
            }

            if (node.TryFindFirstAncestorUpwards<FunctionCallExpressionSyntax>(out var functionCallExpressionSyntax))
            {
                if (functionCallExpressionSyntax.NameToken.Value.Equals("var", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (_functions == null)
            {
                _functions = CssWebData.Index.FunctionNamesSorted.OrderBy(p => p).Select(p => new Completion(p, p, null, _functionGlyph, null)).ToList();
            }

            var span = new Span(node.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            var filter = new IntellisenseFilter(KnownMonikers.Method, "Functions", "F", "Functions");
            builder.AddFilter(filter);
            builder.Add(_functions);
            return true;
        }

        private bool TryCompleteVarArgument(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, SnapshotPoint triggerPoint, CssCompletionsBuilder builder)
        {
            if (!node.TryFindFirstAncestorUpwards<FunctionCallExpressionSyntax>(out var functionCallExpressionSyntax))
            {
                return false;
            }

            if (!functionCallExpressionSyntax.NameToken.Value.Equals("var", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var variables = new Dictionary<string, Completion>();
            var finder = new VariableDefinitionFinder(n =>
            {
                if (n.Position == node.Position)
                {
                    return;
                }

                var nameToken = (IdentifierToken)n.Node;
                var name = nameToken.Value;
                if (variables.ContainsKey(name))
                {
                    return;
                }

                var completion = new Completion(name, name, null, _variableGlyph, null);
                variables.Add(name, completion);
            });
            finder.Visit(syntaxTree);

            var span = node.Node switch
            {
                IdentifierExpressionSyntax identifierSyntax => new Span(node.Position + identifierSyntax.LeadingTrivia.Width(), identifierSyntax.NameToken.Width),
                _ => new Span(triggerPoint.Position, 0)
            };
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.AddFilter(new IntellisenseFilter(KnownMonikers.LocalVariable, "Variables", "V", "Variables"));
            builder.Add(variables.Values);
            return true;
        }

        private bool TryAddPropertyValues(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not IdentifierExpressionSyntax propertyName)
            {
                return false;
            }

            if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var propertySyntax))
            {
                return false;
            }

            if (node.TryFindFirstAncestorUpwards<FunctionCallExpressionSyntax>(out var functionCallExpressionSyntax))
            {
                if (functionCallExpressionSyntax.NameToken.Value.Equals("var", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!CssWebData.Index.Properties.TryGetValue(propertySyntax.NameToken.Value, out var definition))
            {
                return false;
            }

            if (definition.Values is not { Count: > 0 })
            {
                return false;
            }

            if (!_propertyValues.TryGetValue(definition.Name, out var values))
            {
                values = definition.Values.Select(v => new Completion(v.Name, v.Name, v.Description, _enumGlyph, null)).ToList();
                _propertyValues.Add(definition.Name, values);
            }

            var span = new Span(node.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            var filter = new IntellisenseFilter(KnownMonikers.Enumeration, "Values", "V", "Values");
            builder.AddFilter(filter);
            builder.Add(values);
            return true;
        }

        private bool TryAddColors(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, CssCompletionsBuilder builder)
        {
            if (node.Node is not IdentifierExpressionSyntax propertyName)
            {
                return false;
            }

            if (!node.TryFindFirstAncestorUpwards<PropertySyntax>(out var propertySyntax))
            {
                return false;
            }

            if (!CssWebData.Index.Properties.TryGetValue(propertySyntax.NameToken.Value, out var definition))
            {
                return false;
            }

            if ((definition.Syntax == null || !definition.Syntax.Contains("<color>", StringComparison.Ordinal)) &&
                (definition.Restrictions == null || !definition.Restrictions.Contains("color")))
            {
                return false;
            }

            if (_colors == null)
            {
                _colors = CssWebData.Index.NamedColorsSorted.Select(c => new Completion(c.Name, c.Name, c.Hex, _constantGlyph, null)).ToList();
            }

            if (_systemColors == null)
            {
                _systemColors = CssWebData.Index.SystemColorsSorted.Select(c => new Completion(c.Name, c.Name, c.Description, _variableGlyph, null)).ToList();
            }

            if (_allColors == null)
            {
                _allColors = _colors.Concat(_systemColors).OrderBy(c => c.DisplayText).ToList();
            }

            var span = new Span(node.Position + propertyName.LeadingTrivia.Width(), propertyName.NameToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            var filter = new IntellisenseFilter(KnownMonikers.ColorPalette, "Colors", "C", "Colors");
            builder.AddFilter(filter);
            builder.Add(_allColors);
            return true;
        }

        private bool TryCompleteUnits(ITextSnapshot snapshot, SyntaxTree syntaxTree, SnapshotNode node, SnapshotPoint triggerPoint, CssCompletionsBuilder builder)
        {
            if (node.Node is not NumberWithUnitExpressionSyntax propertyName)
            {
                return false;
            }

            if (propertyName.UnitToken.Width == 0)
            {
                return false;
            }

            if (triggerPoint.Position < node.Position + propertyName.NumberToken.Width)
            {
                return false;
            }

            if (_units == null)
            {
                _units = CssWebData.Index.ValueUnitsSorted.Select(u => new Completion(u)).ToList();
            }

            var span = new Span(node.Position + propertyName.LeadingTrivia.Width() + propertyName.NumberToken.Width, propertyName.UnitToken.Width);
            var trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            builder.SetSpan(trackingSpan);
            builder.Add(_units);
            return true;
        }

        private Completion ToCompletion(PropertyData property) => new(property.Name, property.Name, property.Description, _propertyGlyph, null);
        private Completion ToCompletion(HtmlElementData property) => new(property.Name, property.Name, property.Description.Value, _elementGlyph, null);
        private Completion ToCompletion(PseudoClassData property) => new(property.Name, property.Name, property.Description, property.Name == "not" ? _keywordGlyph : _eventGlyph, null);
        private Completion ToCompletion(PseudoElementData property) => new(property.Name, property.Name, property.Description, _element2Glyph, null);
        private Completion ToCompletion(HtmlAttributeData property) => new(property.Name, property.Name, property.Description.Value, property.Name.StartsWith("on", StringComparison.Ordinal) ? _eventGlyph : _attributeGlyph, null);

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
