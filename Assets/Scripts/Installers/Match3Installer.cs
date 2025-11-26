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

        // Services
        Container.Bind<IBombService>()
            .To<BombService>()
            .AsSingle();

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
