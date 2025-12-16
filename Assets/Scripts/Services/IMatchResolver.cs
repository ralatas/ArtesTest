using System.Collections;

public interface IMatchResolver
{
    /// <summary>
    /// Разрешает текущие совпадения: регистрирует бомбы/создаёт бомбу при 4+ и уничтожает обычные совпавшие гемы, затем запускает каскад.
    /// </summary>
    IEnumerator DestroyMatchesCo();
    IEnumerator HandleBombExplosionsAfterCascadeCo();
}