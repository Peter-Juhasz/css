using EditorTest.Extensions;
using EditorTest.Syntax;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;

namespace EditorTest.CodeCompletion;

internal class CssCompletionSet : CompletionSet2
{
    public CssCompletionSet(CssCompletionsBuilder options)
        : base("CSS", "CSS", options.ApplicableTo, options.Completions, null, options.Filters.Count > 1 ? options.Filters : null)
    {
        _options = options;
        _completions.AddRange(options.Completions);
        _filteredCompletions = new(_completions);

        RecalculateBuilders();
    }

    private readonly CssCompletionsBuilder _options;

    private readonly BulkObservableCollection<Completion> _completions = new();
    private readonly FilteredObservableCollection<Completion> _filteredCompletions;

    private readonly BulkObservableCollection<Completion> _builders = new();

    public override IList<Completion> Completions => _filteredCompletions;

    public override IList<Completion> CompletionBuilders => _builders;


    private static readonly Predicate<Completion> MatchAll = _ => true;


    public override void Filter()
    {
        // get typed text
        var typed = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);

        // filter
        if (typed.Length == 0)
        {
            _filteredCompletions.Filter(MatchAll);
            return;
        }

        _filteredCompletions.Filter(c => c.DisplayText.ContainsAtWordStart(typed));
    }

    public override void SelectBestMatch()
    {
        // add builders
        RecalculateBuilders();

        base.SelectBestMatch();
    }

    protected void RecalculateBuilders()
    {
        if (_options.EnableVariableCompletionBuilder)
        {
            RecalculateVariableBuilder();
        }

        if (_options.EnableClassCompletionBuilder || _options.EnableIdCompletionBuilder)
        {
            RecalculateClassOrIdBuilder();
        }
    }

    protected void RecalculateVariableBuilder()
    {
        var typed = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
        if (typed.Length > 2 && typed.StartsWith("--", StringComparison.Ordinal))
        {
            var contains = _completions.Any(c => c.InsertionText == typed);

            if (contains && _builders.Any())
            {
                _builders.Clear();
            }
            else if (_builders.Any())
            {
                _builders.BeginBulkOperation();
                _builders.Clear();
                _builders.Add(new Completion(typed));
                _builders.EndBulkOperation();
            }
            else
            {
                _builders.Add(new Completion(typed));
            }
        }
        else if (_builders.Any())
        {
            _builders.Clear();
        }
    }

    protected void RecalculateClassOrIdBuilder()
    {
        var typed = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
        if (typed.Length > 1)
        {
            var contains = _completions.Any(c => c.InsertionText == typed);

            if (contains && _builders.Any())
            {
                _builders.Clear();
            }
            else if (_builders.Any())
            {
                _builders.BeginBulkOperation();
                _builders.Clear();
                _builders.Add(new Completion(typed));
                _builders.EndBulkOperation();
            }
            else
            {
                _builders.Add(new Completion(typed));
            }
        }
        else if (_builders.Any())
        {
            _builders.Clear();
        }
    }


    private static readonly IReadOnlyList<Span> NoMatches = [];
    public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
    {
        var typed = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
        if (typed.Length == 0)
        {
            return NoMatches;
        }

        List<Span>? spans = null;

        int index = displayText.IndexOf(typed, StringComparison.OrdinalIgnoreCase);
        while (index != -1)
        {
            if (index == 0 || displayText[index - 1] == '-')
            {
                spans ??= new(capacity: 1);
                spans.Add(new(index, typed.Length));
            }

            if (index + typed.Length >= displayText.Length)
            {
                break;
            }

            index = displayText.IndexOf(typed, index + typed.Length, StringComparison.OrdinalIgnoreCase);
        }

        return spans ?? NoMatches;
    }
}
