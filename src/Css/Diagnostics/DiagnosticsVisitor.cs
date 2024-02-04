using Css.Data;
using Css.Source;
using Css.Syntax;
using System;

namespace Css.Diagnostics;

public class DiagnosticsVisitor(Action<Diagnostic> report) : SyntaxLocatorWalker
{
    public override void Visit(ElementSelectorSyntax node)
    {
        var name = node.NameToken.Value;
        if (!name.StartsWith("--", StringComparison.Ordinal) && !HtmlWebData.Index.Elements.ContainsKey(name))
        {
            Report(node.NameToken, "1", Severity.Warning, $"Element '{name}' is unknown.", offset: node.LeadingTrivia.Width());
        }

        base.Visit(node);
    }

    public override void Visit(ClassSelectorSyntax node)
    {
        if (node.NameToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Class name is missing.");
        }

        base.Visit(node);
    }

	public override void Visit(ImportDirectiveSyntax node)
	{
		if (node.Delimiter.IsMissing())
		{
			ReportAfter(node, "1", Severity.Error, $"Space is missing.");
		}
		else if (node.SemicolonToken.IsMissing())
		{
			ReportAfter(node, "1", Severity.Error, $"Semicolon is missing.");
		}

		base.Visit(node);
	}

	public override void Visit(CharsetDirectiveSyntax node)
	{
		if (node.Delimiter.IsMissing())
		{
			ReportAfter(node, "1", Severity.Error, $"Space is missing.");
		}
		else if (node.Delimiter.Text != " ")
		{
			ReportAfter(node.Delimiter, "1", Severity.Error, $"Delimiter must be exactly one space.", offset: node.LeadingTrivia.Width() + node.KeywordToken.Width + node.Delimiter.Width);
		}
		else if (node.CharsetToken.IsMissing())
		{
			ReportAfter(node, "1", Severity.Error, $"Charset is missing.");
		}
		else if (node.CharsetToken is not QuotedStringToken { IsClosed: true, Quote: '"' })
		{
			Report(node.CharsetToken, "1", Severity.Error, $"Charset must be enclosed in double quotes.", offset: node.LeadingTrivia.Width() + node.KeywordToken.Width + node.Delimiter.Width);
		}
		else if (node.SemicolonToken.IsMissing())
		{
			ReportAfter(node, "1", Severity.Error, $"Semicolon is missing.");
		}

		base.Visit(node);
	}

	public override void Visit(IdSelectorSyntax node)
    {
        if (node.NameToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Id is missing.");
        }

        base.Visit(node);
    }

    public override void Visit(AttributeSelectorSyntax node)
    {
        if (node.NameToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Attribute name is missing.");
        }
        else if (node.OperatorToken != null && node.ValueToken == null)
        {
            ReportAfter(node, "1", Severity.Error, $"Attribute value is missing.");
        }
        else if (node.CloseBracketToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Closing bracket is missing.");
        }

        if (node.NameToken.Value == "id" && node.OperatorToken?.Text == "=" && node.ValueToken != null)
        {
            Report(node, "1", Severity.Hint, "Selector can be simplified.");
        }

        if (node.NameToken.Value == "class" && node.OperatorToken?.Text == "~=" && node.ValueToken != null)
        {
            Report(node, "1", Severity.Hint, "Selector can be simplified.");
        }

        base.Visit(node);
    }

    public override void Visit(PseudoClassSelectorSyntax node)
    {
        if (node.NameToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Id is missing.");
        }
        else if (!CssWebData.Index.PseudoClasses.ContainsKey(node.NameToken.Value))
        {
            Report(node.NameToken, "1", Severity.Warning, $"Pseudo class '{node.NameToken.Value}' is unknown.", offset: node.LeadingTrivia.Width() + node.ColonToken.Width);
        }

        base.Visit(node);
    }

    public override void Visit(PseudoElementSelectorSyntax node)
    {
        if (node.NameToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Id is missing.");
        }
        else if (!CssWebData.Index.PseudoElements.ContainsKey(node.NameToken.Value))
        {
            Report(node.NameToken, "1", Severity.Warning, $"Pseudo element '{node.NameToken.Value}' is unknown.", offset: node.LeadingTrivia.Width() + node.ColonsToken.Width);
        }

        base.Visit(node);
    }

    public override void Visit(StringToken node)
    {
        if (node is QuotedStringToken { IsClosed: false })
        {
            ReportAfter(node, "1", Severity.Error, $"String literal is not closed.");
        }

        base.Visit(node);
    }

    public override void Visit(HexColorToken node)
    {
        if (node.Value.Length is not (4 or 7 or 9))
        {
            Report(node, "1", Severity.Error, $"Malformed color literal.");
        }

        base.Visit(node);
    }

    public override void Visit(MultiLineCommentTrivia node)
    {
        if (!node.IsClosed)
        {
            ReportAfter(node, "1", Severity.Error, $"Comment is not closed.");
        }

        base.Visit(node);
    }

    public override void Visit(FunctionCallExpressionSyntax node)
    {
        if (!CssWebData.Index.FunctionNamesSet.Contains(node.NameToken.Value))
        {
            Report(node.NameToken, "1", Severity.Warning, $"Function {node.NameToken.Value} is unknown.", offset: node.LeadingTrivia.Width());
        }

        if (node.CloseParenthesisToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Close parenthesis is missing.");
        }

        base.Visit(node);
    }

    public override void Visit(PropertySyntax node)
	{
		var name = node.NameToken.Value;
		switch (Peek())
        {
            case FontFaceDirectiveSyntax:
                {
					if (!CssWebData.Index.FontFacePropertiesSorted.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
					{
						Report(node.NameToken, node.GetNameSpan(), "1", Severity.Warning, $"Property '{name}' is not allowed in this context.");
					}
				}
                break;

			case ColorProfileDirectiveSyntax:
				{
					if (!CssWebData.Index.ColorProfilePropertiesSorted.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
					{
						Report(node.NameToken, node.GetNameSpan(), "1", Severity.Warning, $"Property '{name}' is not allowed in this context.");
					}
				}
				break;

			case PropertyDirectiveSyntax:
				{
					if (!CssWebData.Index.PropertyPropertiesSorted.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
					{
						Report(node.NameToken, node.GetNameSpan(), "1", Severity.Warning, $"Property '{name}' is not allowed in this context.");
					}
				}
				break;

			default:
                {
					if (!name.StartsWith("--", StringComparison.Ordinal) && !CssWebData.Index.Properties.ContainsKey(name))
					{
						Report(node.NameToken, node.GetNameSpan(), "1", Severity.Warning, $"Property '{name}' is unknown.");
					}
				}
                break;
        }

		if (node.SemicolonToken.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Semicolon is missing.");
        }

        base.Visit(node);
    }

    public override void Visit(RuleDeclarationSyntax node)
    {
        if (node.CloseBrace.IsMissing())
        {
            ReportAfter(node, "1", Severity.Error, $"Close brace is missing.");
        }

        base.Visit(node);
    }


    protected void Report(SyntaxNode node, string id, Severity severity = Severity.Error, string? message = null, int offset = 0)
    {
        report(new(id, new(Consumed + offset, node.Width - node.LeadingTrivia.Width() - node.TrailingTrivia.Width()), severity, message));
    }

    protected void Report(SyntaxToken node, string id, Severity severity = Severity.Error, string? message = null, int offset = 0)
    {
        report(new(id, new(Consumed + offset, node.Width), severity, message));
    }

	protected void Report(SyntaxToken node, RelativeSourceSpan span, string id, Severity severity = Severity.Error, string? message = null)
	{
		report(new(id, new(Consumed + span.RelativePosition, span.Length), severity, message));
	}

	protected void ReportAfter(AbstractSyntaxNode node, string id, Severity severity = Severity.Error, string? message = null, int offset = 0)
    {
        report(new(id, new(Consumed + offset + node.Width - 1, 1), severity, message));
    }
}
