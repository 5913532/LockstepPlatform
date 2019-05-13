﻿using System.Collections.Generic;
using System.Linq;
using Lockstep.Math;
using Entitas;
using Lockstep.Math;
using Lockstep.Logging;
using Lockstep.Game.Interfaces;

namespace Lockstep.Game.Features.Input
{
    public class ExecuteSpawnInput : IExecuteSystem
    {                                              
        private readonly IViewService _viewService;
        private readonly GameContext _gameContext;
        private readonly GameStateContext _gameStateContext;   
        private readonly IGroup<InputEntity> _spawnInputs;    

        private uint _localIdCounter;
        private readonly ActorContext _actorContext;

        public ExecuteSpawnInput(Contexts contexts, ServiceContainer serviceContainer)
        {                                                  
            _viewService = serviceContainer.Get<IViewService>();              
            _gameContext = contexts.game;
            _gameStateContext = contexts.gameState;
            _actorContext = contexts.actor;

            _spawnInputs = contexts.input.GetGroup(
                InputMatcher.AllOf(
                    InputMatcher.EntityConfigId,
                    InputMatcher.ActorId,
                    InputMatcher.Coordinate,
                    InputMatcher.Tick));
        }       

        public void Execute()
        {                                                             
            foreach (var input in _spawnInputs.GetEntities().Where(entity => entity.tick.value == _gameStateContext.tick.value))
            {           
                var actor = _actorContext.GetEntityWithId(input.actorId.value);
                var nextEntityId = actor.entityCount.value;

                var e = _gameContext.CreateEntity();        

                Log.Trace(this, actor.id.value + " -> " + nextEntityId);

                //composite primary key
                e.AddId(nextEntityId);
                e.AddActorId(input.actorId.value);

                //unique id for internal usage
                e.AddLocalId(_localIdCounter);
                
                //some default components that every game-entity must have
                e.AddVelocity(LVector2.zero);
                e.AddPosition(input.coordinate.value);

                _viewService.LoadView(e, input.entityConfigId.value);

                if (e.isNavigable)
                {
                    //TODO: factory method to create entity? 
                    //Default agent settings
                    e.AddRadius(1.ToLFloat());
                    e.AddMaxSpeed(2.ToLFloat());
                    e.AddRvoAgentSettings(LVector2.zero, 5.ToLFloat(), new List<KeyValuePair<LFloat, uint>>());
                }

                actor.ReplaceEntityCount(nextEntityId + 1);
                _localIdCounter += 1;
            }                                                                                    
        }    
    }
}
