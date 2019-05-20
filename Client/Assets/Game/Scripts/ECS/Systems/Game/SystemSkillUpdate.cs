using Entitas;
using Lockstep.Math;

namespace Lockstep.Game.Systems.Game  {
    public class SystemSkillUpdate : IExecuteSystem {
        private readonly GameContext _gameContext;
        readonly IGroup<GameEntity> _skillGroup;

        public SystemSkillUpdate(Contexts contexts, IServiceContainer serviceContainer){
            _gameContext = contexts.game;

            _skillGroup = _gameContext.GetGroup(GameMatcher.AllOf(
                GameMatcher.LocalId,
                GameMatcher.Skill));
        }

        public void Execute(){
            foreach (var entity in _skillGroup.GetEntities()) {
                var skill = entity.skill;
                skill.cdTimer += Define.DeltaTime;
                if (skill.cdTimer < 0) {
                    skill.cdTimer = LFloat.zero;
                }
            }
        }
    }
}