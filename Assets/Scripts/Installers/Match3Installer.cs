using UnityEngine;
using Zenject;

public class Match3Installer : MonoInstaller
{
    public override void InstallBindings()
    {
        // Game Logic
        Container.Bind<SC_GameLogic>()
            .FromComponentInHierarchy()
            .AsSingle();

        // Board
        Container.Bind<GameBoard>()
            .AsSingle()
            .WithArguments(
                SC_GameVariables.Instance.colsSize,
                SC_GameVariables.Instance.rowsSize
            );

        Container.Bind<IBombService>()
            .To<BombService>()
            .AsSingle();

        Container.Bind<IGemSpawnService>()
            .To<GemSpawnService>()
            .AsSingle();

        Container.Bind<IHintService>()
            .To<HintService>()
            .AsSingle();

        Container.Bind<IBombBehavior>()
            .To<RocketBombBehavior>()
            .AsTransient();

        Container.Bind<IBombBehavior>()
            .To<AreaBombBehavior>()
            .AsTransient();

        Container.Bind<IBombBehavior>()
            .To<DiscoBallBehavior>()
            .AsTransient();

        Container.Bind<IScoreService>()
            .To<ScoreService>()
            .AsSingle();

        Container.Bind<IMatchResolver>()
            .To<MatchResolver>()
            .AsSingle();

        Container.Bind<IBoardRefillService>()
            .To<BoardRefillService>()
            .AsSingle();

        Container.Bind<IInputService>()
            .To<InputService>()
            .AsSingle();
    }
}
