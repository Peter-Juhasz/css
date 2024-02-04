using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace Css.Hosting.VisualStudio.CodeCompletion;

public class CssCompletionsBuilder
{
    public static readonly CssCompletionsBuilder Default = new();

    private static readonly IComparer<Completion> Comparer = new CompletionComparer();

    public ITrackingSpan ApplicableTo { get; private set; }

    private readonly List<Completion> _completions = new List<Completion>();
    public IList<Completion> Completions => _completions;

    private readonly List<IIntellisenseFilter> _filters = new List<IIntellisenseFilter>();
    public IReadOnlyList<IIntellisenseFilter> Filters => _filters;


    public void SetSpan(ITrackingSpan span) => ApplicableTo = span;


    public void Add(IReadOnlyCollection<Completion> completions)
    {
        _completions.AddRange(completions);
    }


    public void AddFilter(IIntellisenseFilter filter)
    {
        _filters.Add(filter);
    }

    public void Sort()
    {
        _completions.Sort(Comparer);
    }


    public bool EnableVariableCompletionBuilder { get; set; } = false;

    public bool EnableClassCompletionBuilder { get; set; } = false;
    
    public bool EnableIdCompletionBuilder { get; set; } = false;
}

internal sealed class CompletionComparer : IComparer<Completion>
{
    public int Compare(Completion x, Completion y) => x.DisplayText.CompareTo(y.DisplayText);
}
