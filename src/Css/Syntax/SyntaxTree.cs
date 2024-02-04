namespace Css.Syntax;

public record class SyntaxTree(RootSyntax Root)
{
    public SyntaxNodeSearchResult FindNodeAt(int position)
    {
        var walker = new SyntaxNodeFromPointLocatorWalker(position);
        walker.Visit(this);
        return walker.GetResult();
    }
}
