using Entitas;
using Lockstep.Math;

namespace Lockstep.Game.Systems.Game {
    public class SystemApplyEnemyDestroyEffect : BaseSystem, IExecuteSystem {
        readonly IGroup<GameEntity> _destroyedGroup;

        public SystemApplyEnemyDestroyEffect(Contexts contexts, IServiceContainer serviceContainer) : base(contexts,
            serviceContainer){
            _destroyedGroup = contexts.game.GetGroup(GameMatcher.AllOf(
                GameMatcher.Destroyed,
                GameMatcher.LocalId,
                GameMatcher.TagEnemy));
        }


        public void Execute(){
            foreach (var entity in _destroyedGroup.GetEntities()) {
                var tank = entity.unit;
                _resourceService.ShowDiedEffect(entity.move.pos);
                _audioService.PlayClipDied();
                var killerGameEntity = _gameContext.GetEntityWithLocalId(tank.killerLocalId);
                if (entity.hasDropRate) {
                    this._unitService.DropItem(entity.dropRate.value);
                }

                if (killerGameEntity == null) return;
                var killerActor = _actorContext.GetEntityWithId(killerGameEntity.actorId.value);
                killerActor.score.value += (tank.detailType + 1) * 100;
            }
        }
    }
}