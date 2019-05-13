#define DEBUG_FRAME_DELAY
using Lockstep.Serialization;

namespace NetMsg.Game.Tank{
    public partial class PlayerInput : BaseFormater {
        public byte playerID;
        public uint Tick;
        public List<InputCmd> allInputs = new List<InputCmd>();
#if DEBUG_FRAME_DELAY
        public float timeSinceStartUp;
#endif
        public PlayerInput(byte actorID, uint tick, params InputCmd[] inputs){
            
        }

        /// <summary>
        /// TODO     合并 输入
        /// </summary>
        /// <param name="inputb"></param>
        public void Combine(PlayerInput inputb){ }

        public override void Serialize(Serializer writer){
#if DEBUG_FRAME_DELAY
            writer.Put(timeSinceStartUp);
#endif
            writer.Put(playerID);
            int count = 0;
            for (; count < allInputs.Length; count++) {
                if (allInputs[count] == null) {
                    break;
                }
            }

            writer.Put((byte) count);
            for (int i = 0; i < count; i++) {
                allInputs[i].Serialize(writer);
            }
        }

        public override void Deserialize(Deserializer reader){
#if DEBUG_FRAME_DELAY
            timeSinceStartUp = reader.GetFloat();
#endif
            playerID = reader.GetByte();
            int count = reader.GetByte();
            for (int i = 0; i < count; i++) {
                allInputs[i] = new InputCmd();
                allInputs[i].Deserialize(reader);
            }
        }
    }
}