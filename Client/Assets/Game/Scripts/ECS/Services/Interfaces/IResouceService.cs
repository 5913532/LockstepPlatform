using Lockstep.Math;
using UnityEngine;

namespace Lockstep.Game {
    public interface IResourceService :IService {
        void ShowDiedEffect(LVector2 pos);
        void ShowBornEffect(LVector2 pos);
    }
}