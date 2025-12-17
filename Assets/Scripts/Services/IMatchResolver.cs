using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Resolves color matches and destroys gems. Also provides explicit destruction API for bomb explosions.
/// </summary>
public interface IMatchResolver
{
    IEnumerator DestroyMatchesCo();
    IEnumerator DestroyGemsCo(IReadOnlyCollection<SC_Gem> gemsToDestroy);
}
