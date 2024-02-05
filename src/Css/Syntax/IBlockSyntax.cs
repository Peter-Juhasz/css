using Css.Source;

namespace Css.Syntax;

public interface IBlockSyntax
{
    PunctationToken OpenBrace { get; }
	
    PunctationToken CloseBrace { get; }

    RelativeSourceSpan GetBlockSpan();
}
