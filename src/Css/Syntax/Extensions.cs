using Css.Source;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace EditorTest.Syntax;

public static partial class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any<T>(this IReadOnlyList<T> list) => list.Count > 0;

    public static bool Any<T>(this IReadOnlyList<T> list, Func<T, bool> predicate)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(this StringSegment token) => token.Length > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any(this SyntaxToken token) => token.Text.Length > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMissing(this SyntaxToken token) => token.Text.Length == 0;

    public static bool TryFindFirstAncestorUpwards<TSyntax>(this SnapshotNode node, out TSyntax ancestor) where TSyntax : SyntaxNode
    {
        if (node.Ancestors == null)
        {
            ancestor = default;
            return false;
        }

        for (var i = 0; i < node.Ancestors.Count; i++)
        {
            if (node.Ancestors[i] is TSyntax found)
            {
                ancestor = found;
                return true;
            }
        }

        ancestor = default;
        return false;
    }

    public static int Width(this IImmutableList<SyntaxTrivia> list)
    {
        if (list.Count == 0)
        {
            return 0;
        }

        if (list.Count == 1)
        {
            return list[0].Width;
        }

        var width = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            width += item.Width;
        }

        return width;
    }

    public static bool IsWhiteSpace(this IImmutableList<SyntaxTrivia> list)
    {
        if (list.Count == 0)
        {
            return false;
        }

        if (list.Count == 1)
        {
            return list[0] is WhiteSpaceTrivia;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not WhiteSpaceTrivia)
            {
                return false;
            }
        }

        return true;
    }

    public static bool HasComment(this IImmutableList<SyntaxTrivia> list)
    {
        if (list.Count == 0)
        {
            return false;
        }

        if (list.Count == 1)
        {
            return list[0] is CommentTrivia;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is CommentTrivia)
            {
                return true;
            }
        }

        return false;
    }

    public static RelativeSourceSpan GetLeadingTriviaExtent(this SyntaxNode node) => new(node, 0, node.LeadingTrivia.Width());

    public static RelativeSourceSpan GetTrailingTriviaExtent(this SyntaxNode node) => new(node, node.Width - node.TrailingTrivia.Width(), node.TrailingTrivia.Width());

    public static StringSegment Subsegment(this StringSegment segment, Range range) => range.GetOffsetAndLength(segment.Length) is (int offset, int length) ? segment.Subsegment(offset, length) : throw new InvalidOperationException();
    
    public static StringSegment Subsegment(this string segment, int index) => new StringSegment(segment).Subsegment(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char IndexSafe(this string str, int index) => index < 0 || index >= str.Length ? default : str[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char IndexSafe(this StringSegment str, int index) => index < 0 || index >= str.Length ? default : str[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char PeekSafe(this StringSegment str, int index = 0, int relative = 0) => str.IndexSafe(index + relative);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char PeekNextSafe(this StringSegment str, int index = 0, int relative = 1) => str.IndexSafe(index + relative);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char PeekPreviousSafe(this StringSegment str, int index, int relative = 1) => str.IndexSafe(index - relative);

    public static int IndexOf(this StringSegment str, string subject, int start = 0, StringComparison comparison = StringComparison.Ordinal)
    {
        var index = str.AsSpan(start).IndexOf(subject.AsSpan(), comparison);
        if (index == -1)
        {
            return -1;
        }

        return start + index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string str, string subject, StringComparison comparison) => str.IndexOf(subject, comparison) != -1;
}