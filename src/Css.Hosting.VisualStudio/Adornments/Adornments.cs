using Css.Data;
using Css.Syntax;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace Css.Hosting.VisualStudio.Outlining;

[Export(typeof(ITaggerProvider))]
[TagType(typeof(IntraTextAdornmentTag))]
[ContentType(EditorClassifier1.ContentType)]
internal sealed class CssColorAdornmentTaggerProvider : ITaggerProvider
{
    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        return buffer.Properties.GetOrCreateSingletonProperty(
            creator: () => new ColorAdornmentTagger(buffer)
        ) as ITagger<T>;
    }


    private sealed class ColorAdornmentTagger : ITagger<IntraTextAdornmentTag>
    {
        public ColorAdornmentTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.ChangedLowPriority += OnBufferChanged;
            Invalidate(_buffer.CurrentSnapshot);
        }

        private readonly ITextBuffer _buffer;

        private CacheEntry? _previouslyReported;
        private CacheEntry? _currentToReport;

        private readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

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

        private IReadOnlyList<ITagSpan<IntraTextAdornmentTag>> GetTags(ITextSnapshot snapshot, SyntaxTree syntaxTree)
        {
            var tags = new List<ITagSpan<IntraTextAdornmentTag>>();
            var walker = new ColorsFinder(found =>
            {
                var span = new SnapshotSpan(snapshot, found.Position, 0);

                Color? color = null;

                switch (found.Node)
                {
                    case IdentifierToken named when CssWebData.Index.NamedColors.TryGetValue(named.Value, out var colorData):
                        if (colorData.Hex != null)
                        {
                            var value = Int32.Parse(colorData.Hex[1..], System.Globalization.NumberStyles.HexNumber);
                            color = Color.FromRgb((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
                        }
                        break;

                    case HexColorToken hex:
                        {
                            var value = Int32.Parse(hex.Value[1..], System.Globalization.NumberStyles.HexNumber);
                            color = Color.FromRgb((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF));
                        }
                        break;
                }

                if (color == null)
                {
                    return;
                }

                if (!_brushCache.TryGetValue(color.Value, out var brush))
                {
                    brush = new SolidColorBrush(color.Value);
                    brush.Freeze();
                    _brushCache[color.Value] = brush;
                }

                var adornment = new Border()
                {
                    Background = brush,
                    BorderThickness = new(1),
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    Width = 10,
                    Height = 10,
                    Margin = new(0, 0, 2, 3),
                };
                tags.Add(new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(adornment, null)));
            });
            walker.Visit(syntaxTree);
            return tags;
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans[0].Snapshot;
            var current = _currentToReport;
            if (current.HasValue && current.Value.Version.VersionNumber == snapshot.Version.VersionNumber)
            {
                return current.Value.Tags;
            }

            return Array.Empty<TagSpan<IntraTextAdornmentTag>>();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private record struct CacheEntry(ITextVersion Version, IReadOnlyList<ITagSpan<IntraTextAdornmentTag>> Tags);
    }
}
