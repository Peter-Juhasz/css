using Css.ReferenceHighlight;
using Css.Data;
using Css.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Css.Hosting.VisualStudio.ReferenceHighlight;

[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(ITextMarkerTag))]
[ContentType(EditorClassifier1.ContentType)]
internal sealed class CssHighlightReferencesTaggerProvider : IViewTaggerProvider
{
    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    {
        return textView.Properties.GetOrCreateSingletonProperty(
            creator: () => new Tagger(textView)
        ) as ITagger<T>;
    }


    private sealed class Tagger : ITagger<ITextMarkerTag>, IDisposable
    {
        public Tagger(ITextView view)
        {
            _view = view;
            _view.Caret.PositionChanged += OnCaretPositionChanged;
            _view.TextBuffer.ChangedLowPriority += OnPostChanged;
        }

        private readonly ITextView _view;
        private readonly ReferenceHighlighter highlighter = new ReferenceHighlighter();

        private static readonly ITextMarkerTag DefinitionTag = new TextMarkerTag("MarkerFormatDefinition/HighlightedDefinition");
        private static readonly ITextMarkerTag ReferenceTag = new TextMarkerTag("MarkerFormatDefinition/HighlightedReference");

        private CacheEntry? _previouslyReported;
        private CacheEntry? _currentToReport;


        private void OnPostChanged(object sender, EventArgs e)
        {
            Invalidate(_view.Caret.Position.BufferPosition);
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            Invalidate(e.NewPosition.BufferPosition);
        }

        private void Invalidate(SnapshotPoint point)
        {
            var snapshot = point.Snapshot;

            // re-requested same point, no changes
            var currentToReport = _currentToReport;
            if (currentToReport.HasValue && currentToReport.Value.Version.VersionNumber == snapshot.Version.VersionNumber && currentToReport.Value.TriggerPoint == point)
            {
                return;
            }

            // re-requested same point, no changes
            var previous = _previouslyReported;
            if (previous.HasValue && previous.Value.Version.VersionNumber == snapshot.Version.VersionNumber && previous.Value.TriggerPoint == point)
            {
                _currentToReport = previous;
                return;
            }

            // locate syntax node
            var syntaxTree = snapshot.GetSyntaxTree();
            var matches = syntaxTree.FindNodeAt(point);

            // no matches
            if (!matches.Any())
            {
                _currentToReport = new(snapshot.Version, point, matches, []);

                if (previous != null && previous.Value.Tags.Any())
                {
                    var pmin = previous.Value.Tags.Min(t => t.Span.Span.Start);
                    var pmax = previous.Value.Tags.Max(t => t.Span.Span.End);
                    this.TagsChanged?.Invoke(this, new(new(snapshot, pmin, pmax - pmin)));
                }

                return;
            }

            // same matches
            if (previous?.Matches == matches)
            {
                _currentToReport = previous;
                return;
            }

            // get tags
            var tags = GetTags(syntaxTree, matches, point);
            _currentToReport = new(snapshot.Version, point, matches, tags);

            // no tags
            if (!tags.Any())
            {
                _currentToReport = new(snapshot.Version, point, matches, []);

                if (previous != null && previous.Value.Tags.Any())
                {
                    var pmin = previous.Value.Tags.Min(t => t.Span.Span.Start);
                    var pmax = previous.Value.Tags.Max(t => t.Span.Span.End);
                    this.TagsChanged?.Invoke(this, new(new(snapshot, pmin, pmax - pmin)));
                }

                return;
            }

            // no tags previously
            if (previous == null || !previous.Value.Tags.Any())
            {
                var pmin = tags.Min(t => t.Span.Span.Start);
                var pmax = tags.Max(t => t.Span.Span.End);
                this.TagsChanged?.Invoke(this, new(new(snapshot, pmin, pmax - pmin)));
                return;
            }

            // compute difference
            var previousTags = previous.Value.Tags;
            if (tags.Count == previousTags.Count)
            {
                var allMatches = true;
                for (var i = 0; i < tags.Count; i++)
                {
                    if (tags[i].Span != previousTags[i].Span)
                    {
                        allMatches = false;
                        break;
                    }
                }

                if (allMatches)
                {
                    return;
                }
            }

            // notify changes
            var min = Math.Min(previousTags.Min(m => m.Span.Span.Start), tags.Min(m => m.Span.Span.Start));
            var max = Math.Max(previousTags.Max(m => m.Span.Span.End), tags.Max(m => m.Span.Span.End));
            this.TagsChanged?.Invoke(this, new(new(snapshot, min, max - min)));
        }

        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var current = _currentToReport;
            if (current.HasValue && current.Value.Version.VersionNumber == snapshot.Version.VersionNumber)
            {
                _previouslyReported = current;
                return current.Value.Tags;
            }

            return Array.Empty<TagSpan<ITextMarkerTag>>();
        }

        public IReadOnlyList<ITagSpan<ITextMarkerTag>> GetTags(SyntaxTree syntaxTree, SyntaxNodeSearchResult matches, SnapshotPoint point)
        {
            var spans = highlighter.GetHighlightedSpans(syntaxTree, matches, point.ToSourcePoint());
            if (spans.Count == 0)
            {
                return Array.Empty<ITagSpan<ITextMarkerTag>>();
            }

            return spans.Select(span => new TagSpan<ITextMarkerTag>(span.ToSnapshotSpan(point.Snapshot), ReferenceTag)).ToList();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            _view.Caret.PositionChanged -= OnCaretPositionChanged;
            _view.TextBuffer.PostChanged -= OnPostChanged;
        }

        private record struct CacheEntry(ITextVersion Version, SnapshotPoint TriggerPoint, SyntaxNodeSearchResult Matches, IReadOnlyList<ITagSpan<ITextMarkerTag>> Tags);
    }
}
