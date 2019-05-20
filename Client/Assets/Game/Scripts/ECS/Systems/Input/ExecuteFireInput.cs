﻿using System.Collections.Generic;
using System.Linq;
using Lockstep.Math;
using Entitas;
using Lockstep.Math;
using Lockstep.Logging;
using Lockstep.Game.Interfaces;

namespace Lockstep.Game.Features.Input {
    public class ExecuteFireInput : IExecuteSystem {
        private readonly GameContext _gameContext;
        readonly IGroup<InputEntity> _moveInput;
        private readonly GameStateContext _gameStateContext;

        public ExecuteFireInput(Contexts contexts, IServiceContainer serviceContainer)
        {                                             
            _gameContext = contexts.game;
            _gameStateContext = contexts.gameState;                        

            _moveInput = contexts.input.GetGroup(InputMatcher.AllOf(
                InputMatcher.Fire,
                InputMatcher.ActorId, 
                InputMatcher.Tick));
        }    

        public void Execute()
        {
            foreach (var input in _moveInput.GetEntities().
                Where(entity => entity.tick.value == _gameStateContext.tick.value))
            {
                var gameEntity = _gameContext.GetEntityWithLocalId(input.actorId.value);
                gameEntity.isFireRequest = true;
            }
        }
    }
}