using System.Collections.Generic;

public class BombService : IBombService
{
    private readonly List<IBombBehavior> behaviors;

    public BombService(IEnumerable<IBombBehavior> behaviors)
    {
        this.behaviors = behaviors != null ? new List<IBombBehavior>(behaviors) : new List<IBombBehavior>();

        // Ensure we always have a default set of behaviors to fall back on.
        if (this.behaviors.Count == 0)
        {
            this.behaviors.Add(new RocketBombBehavior());
            this.behaviors.Add(new AreaBombBehavior());
        }
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        foreach (var behavior in behaviors)
        {
            if (!behavior.CanHandle(bomb))
                continue;

            foreach (var target in behavior.GetExplosionTargets(bomb, board))
                yield return target;

            // We stop after the first matching behavior.
            yield break;
        }
    }
}
