using Entitas;
using Lockstep.ECS.Game;

namespace Lockstep.Game {
    public static class ItemUtil {
        //TODO
        public static void TriggerEffect(EItemType type,ActorEntity actor, GameContext gameContext){
        }

        static void OnTriggerBoom(Tank trigger){
            foreach (var tank in GameManager.Instance.allEnmey) {
                tank.health = 0;
                tank.killer = trigger;
            }
        }

        static void OnTriggerUpgrade(Tank trigger){
            GameManager.Instance.Upgrade(trigger, 1);
        }

        static void OnTriggerAddLife(Tank trigger){
            var gameMgr = GameManager.Instance;
            var info = gameMgr.GetPlayerFormTank(trigger);
            if (info != null) {
                info.remainPlayerLife++;
                if (gameMgr.OnLifeCountChanged != null) {
                    gameMgr.OnLifeCountChanged(info);
                }
            }
        }
    }
}