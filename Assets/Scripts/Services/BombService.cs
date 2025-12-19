using System.Collections.Generic;

public class BombService : IBombService
{
    private readonly List<IBombBehavior> behaviors;
    private readonly List<IBombCreationBehavior> creationBehaviors;

    public BombService(IEnumerable<IBombBehavior> behaviors)
    {
        this.behaviors = behaviors != null ? new List<IBombBehavior>(behaviors) : new List<IBombBehavior>();

        // Ensure we always have a default set of behaviors to fall back on.
        if (this.behaviors.Count == 0)
        {
            this.behaviors.Add(new RocketBombBehavior());
            this.behaviors.Add(new AreaBombBehavior());
        }

        creationBehaviors = new List<IBombCreationBehavior>();
        foreach (var behavior in this.behaviors)
        {
            if (behavior is IBombCreationBehavior creationBehavior)
                creationBehaviors.Add(creationBehavior);
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

    public void MakeBomb(SC_Gem gem, GlobalEnums.RocketDirection direction)
    {
        if (gem == null)
            return;

        IBombCreationBehavior creator = null;

        if (direction == GlobalEnums.RocketDirection.None)
        {
            foreach (var creation in creationBehaviors)
            {
                if (creation is AreaBombBehavior)
                {
                    creator = creation;
                    break;
                }
            }
        }
        else
        {
            foreach (var creation in creationBehaviors)
            {
                if (creation is RocketBombBehavior)
                {
                    creator = creation;
                    break;
                }
            }
        }

        // Fallbacks if no matching behavior was bound.
        if (creator == null)
            creator = direction == GlobalEnums.RocketDirection.None ? new AreaBombBehavior() : (IBombCreationBehavior)new RocketBombBehavior();

        creator.MakeBomb(gem, direction);
    }
}
