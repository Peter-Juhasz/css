using Css.Outlining;
using Css.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Css.Hosting.VisualStudio.Outlining
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(EditorClassifier1.ContentType)]
    internal sealed class CssStructureTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                creator: () => new Tagger(buffer)
            ) as ITagger<T>;
        }


        private sealed class Tagger : ITagger<IStructureTag>
        {
            public Tagger(ITextBuffer buffer)
            {
                _buffer = buffer;
                _buffer.ChangedLowPriority += OnBufferChanged;
                Invalidate(_buffer.CurrentSnapshot);
            }

            private readonly ITextBuffer _buffer;

            private CacheEntry? _previouslyReported;
            private CacheEntry? _currentToReport;

            private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
            {
                Invalidate(e.After);
            }

            private void Invalidate(ITextSnapshot snapshot)
            {
                // rerequested same point, no changes
                var currentToReport = _currentToReport;
                if (currentToReport.HasValue && currentToReport.Value.Version.VersionNumber == snapshot.Version.VersionNumber)
                {
                    return;
                }

                // rerequested same point, no changes
                var previous = _previouslyReported;
                if (previous.HasValue && previous.Value.Version.VersionNumber == snapshot.Version.VersionNumber)
                {
                    _currentToReport = previous;
                    return;
                }

                // get tags
                var syntaxTree = snapshot.GetSyntaxTree();
                var tags = GetTags(snapshot, syntaxTree);
                _currentToReport = new(snapshot.Version, tags);

                // no tags
                if (!tags.Any())
                {
                    _currentToReport = new(snapshot.Version, []);

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

            private static IReadOnlyList<ITagSpan<IStructureTag>> GetTags(ITextSnapshot snapshot, SyntaxTree syntaxTree)
            {
                var tags = new List<ITagSpan<IStructureTag>>();
                var walker = new StructureSyntaxWalker(found =>
                {
                    if (found.Node.OpenBrace.IsMissing() || (found.Node.Selectors.Count == 0 && found.Node.CloseBrace.IsMissing()))
                    {
                        return;
                    }

                    var openBrace = new SnapshotPoint(snapshot, found.Position + found.Node.LeadingTrivia.Width() + found.Node.Selectors.Width);
                    var closeBrace = new SnapshotPoint(snapshot, found.Position + found.Node.Width - found.Node.TrailingTrivia.Width());
                    var collapsibleSpan = new SnapshotSpan(openBrace, closeBrace);

                    tags.Add(new TagSpan<IStructureTag>(collapsibleSpan, new StructureTag(
                        snapshot: snapshot,
                        outliningSpan: collapsibleSpan,
                        headerSpan: new Span(found.Position + found.Node.LeadingTrivia.Width(), found.Node.Selectors.Width),
                        isCollapsible: true
                    )));
                });
                walker.Visit(syntaxTree);
                return tags;
            }

            public IEnumerable<ITagSpan<IStructureTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                var snapshot = spans[0].Snapshot;
                var current = _currentToReport;
                if (current.HasValue && current.Value.Version.VersionNumber == snapshot.Version.VersionNumber)
                {
                    return current.Value.Tags;
                }

                return Array.Empty<TagSpan<IStructureTag>>();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            private record struct CacheEntry(ITextVersion Version, IReadOnlyList<ITagSpan<IStructureTag>> Tags);
        }
    }
}
