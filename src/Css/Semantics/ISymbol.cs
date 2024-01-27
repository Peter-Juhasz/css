namespace Css.Semantics;

public interface ISymbol
{
}

public record class PropertySymbol(string Name) : ISymbol;


