using EditorTest.Syntax;
using Microsoft.VisualStudio.Text;
using System;

namespace EditorTest;

internal sealed class SyntaxTreeCache(int size = 3)
{
    private readonly CacheEntry[] _cache = new CacheEntry[size];
    private int _index = 0;

    public bool TryGet(ITextVersion version, out SyntaxTree tree)
    {
        for (int i = 0; i < _cache.Length; i++)
        {
            var item = _cache[i];
            if (item == default)
            {
                continue;
            }

            if (item.Version.VersionNumber == version.VersionNumber)
            {
                tree = item.SyntaxTree;
                return true;
            }
        }

        tree = null;
        return false;
    }

    public void Add(ITextVersion version, SyntaxTree SyntaxTree)
    {
        _cache[_index] = new CacheEntry(version, SyntaxTree);
        _index = (_index + 1) % _cache.Length;
    }

    public SyntaxTree GetOrCreate(ITextVersion version, Func<SyntaxTree> create)
    {
        if (TryGet(version, out var tree))
        {
            return tree;
        }

        tree = create();
        Add(version, tree);
        return tree;
    }

    private record struct CacheEntry(ITextVersion Version, SyntaxTree SyntaxTree);
}
