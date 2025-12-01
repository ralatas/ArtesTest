using System.Collections;

public interface IMatchResolver
{
    IEnumerator DestroyMatchesCo();
    IEnumerator HandleBombExplosionsAfterCascadeCo();
}