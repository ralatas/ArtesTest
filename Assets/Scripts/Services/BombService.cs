using System.Collections.Generic;

public class BombService : IBombService
{
    private readonly Dictionary<System.Type, IBombBehavior> behaviors;

    public BombService(IEnumerable<IBombBehavior> behaviors)
    {
        this.behaviors = new Dictionary<System.Type, IBombBehavior>();

        if (behaviors != null)
        {
            foreach (var behavior in behaviors)
            {
                if (behavior == null)
                    continue;

                var key = behavior.GetType();
                if (!this.behaviors.ContainsKey(key))
                    this.behaviors.Add(key, behavior);
            }
        }

        // Ensure defaults exist.
        if (!this.behaviors.ContainsKey(typeof(RocketBombBehavior)))
            this.behaviors[typeof(RocketBombBehavior)] = new RocketBombBehavior();
        if (!this.behaviors.ContainsKey(typeof(AreaBombBehavior)))
            this.behaviors[typeof(AreaBombBehavior)] = new AreaBombBehavior();
        if (!this.behaviors.ContainsKey(typeof(DiscoBallBehavior)))
            this.behaviors[typeof(DiscoBallBehavior)] = new DiscoBallBehavior();
        if (!this.behaviors.ContainsKey(typeof(HelicopterBombBehavior)))
            this.behaviors[typeof(HelicopterBombBehavior)] = new HelicopterBombBehavior();
    }

    public IEnumerable<SC_Gem> GetExplosionTargets(SC_Gem bomb, GameBoard board)
    {
        if (bomb == null || board == null)
            yield break;

        foreach (var behavior in behaviors.Values)
        {
            if (!behavior.CanHandle(bomb))
                continue;

            foreach (var target in behavior.GetExplosionTargets(bomb, board))
                yield return target;

            yield break;
        }
    }
}
