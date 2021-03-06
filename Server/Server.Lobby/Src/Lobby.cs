using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LiteNetLib;
using Lockstep.Serialization;
using Lockstep.Logging;
using NetMsg.Lobby;
using Server.Common;

namespace Lockstep.Logic.Server {
    public class Lobby : ILobby {
        //TCP
        private Dictionary<long, Player> playerID2Player = new Dictionary<long, Player>();
        private Dictionary<int, NetPeer> netId2NetPeer = new Dictionary<int, NetPeer>();
        private Dictionary<int, Player> netID2Player = new Dictionary<int, Player>();


        private Dictionary<int, IRoom> roomId2Room = new Dictionary<int, IRoom>();
        private List<IRoom> _allRooms = new List<IRoom>();
        private Dictionary<int, List<IRoom>> gameId2Rooms = new Dictionary<int, List<IRoom>>();

        private static long PlayerAutoIncID = 1;
        private static int RoomAutoIncID = 0;
        private const int MAX_NAME_LEN = 30;

        public NetServer serverLobby;
        public NetServer serverRoom;

        public const byte MAX_HANDLER_IDX = (byte) EMsgCL.EnumCount;
        public const byte INIT_MSG_IDX = (byte) EMsgCL.C2L_InitMsg;
        private DealNetMsg[] allMsgDealFuncs = new DealNetMsg[(int) EMsgCL.EnumCount];

        private delegate IRoom FuncCreateRoom();

        private delegate void DealNetMsg(Player player, Deserializer reader);

        private Dictionary<int, FuncCreateRoom> _roomFactoryFuncs = new Dictionary<int, FuncCreateRoom>();

        //TODO read from config
        string RoomType2DllPath(int type){
            return "Game.Tank" + ".dll";
        }


        #region LifeCycle

        private int _udpPort;

        public void DoStart(int tcpPort, int udpPort){
            RegisterMsgHandlers();
            serverLobby = new NetServer(Define.ClientKey);
            serverLobby.DataReceived += OnDataReceived;
            serverLobby.ClientConnected += OnClientConnected;
            serverLobby.ClientDisconnected += OnCilentDisconnected;
            serverLobby.Run(tcpPort);
            this._udpPort = udpPort;
            serverRoom = new NetServer(Define.ClientKey);
            serverRoom.DataReceived += OnDataReceivedRoom;
            serverRoom.ClientConnected += OnClientConnectedRoom;
            serverRoom.ClientDisconnected += OnCilentDisconnectedRoom;
            serverRoom.Run(udpPort);
            Debug.Log($"Listen tcpPort {tcpPort} udpPort {udpPort}");
        }

        public void OnDataReceivedRoom(NetPeer peer, byte[] data){
            int netID = peer.Id;
            try {
                var reader = new Deserializer(Compressor.Decompress(data));
                var playerID = reader.GetLong();
                var player = GetPlayer(playerID);
                if (player.gameSock == null) {
                    player.gameSock = peer;
                }

                var room = player.room;
                if (room == null) {
                    Debug.LogError($"MsgError:Player {player.PlayerId} not in room");
                    return;
                }

                room.OnRecvMsg(player, reader);
            }
            catch (Exception e) {
                Debug.LogError($"netID{netID} parse msg Error:{e.ToString()}");
            }
        }

        public void OnClientConnectedRoom(object objPeer){
            var peer = (NetPeer) objPeer;
            Debug.Log($"OnClientConnected netID = {peer.Id}");
        }

        public void OnCilentDisconnectedRoom(object objPeer){
            var peer = (NetPeer) objPeer;
            Debug.Log($"OnCilentDisconnected netID = {peer.Id}");
            var player = GetPlayerRoom(peer.Id);
            if (player != null) {
                LeaveRoom(player.PlayerId);
                RemovePlayer(peer.Id);
            }
        }


        public void DoUpdate(int deltaTime){
            foreach (var room in _allRooms) {
                try {
                    room?.DoUpdate(deltaTime);
                }
                catch (Exception e) {
                    Debug.LogError(e.ToString());
                }
            }
        }

        public void DoDestroy(){ }

        public void PollEvents(){
            serverLobby?.PollEvents();
            serverRoom?.PollEvents();
        }

        #endregion

        #region rooms

        public List<IRoom> GetRooms(int roomType){
            return gameId2Rooms.GetRefVal(roomType);
        }

        public IRoom GetRoom(int roomId){
            return roomId2Room.GetRefVal(roomId);
        }

        public IRoom GetRoomByUserID(int id){
            var player = GetPlayer(id);
            if (player != null) {
                return player.room;
            }

            return null;
        }

        public void RemoveRoom(IRoom room){
            roomId2Room.Remove(room.RoomId);
            _allRooms.Remove(room);
            gameId2Rooms[room.TypeId].Remove(room);
            if (gameId2Rooms[room.TypeId].Count == 0) {
                gameId2Rooms.Remove(room.TypeId);
            }

            room.DoDestroy();
        }

        /// Create From Dll by reflect 
        private IRoom CreateRoom(int type){
            //TODO Pool
            if (_roomFactoryFuncs.TryGetValue(type, out FuncCreateRoom _func)) {
                return _func?.Invoke();
            }

            var path = RoomType2DllPath(type);
            if (path == null) {
                return null;
            }

            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            var assembly = Assembly.LoadFrom(dllPath);
            Debug.Log("Load dll " + dllPath);
            if (assembly == null) {
                Debug.LogError("Load dll failed  " + dllPath);
                _roomFactoryFuncs[type] = null;
                return null;
            }

            var types = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IRoom))).ToArray();
            if (types.Length != 1) {
                Debug.LogError("dll do not have type of IRoom :" + dllPath);
                _roomFactoryFuncs[type] = null;
                return null;
            }

            FuncCreateRoom factory = () => { return (IRoom) System.Activator.CreateInstance(types[0], true); };
            _roomFactoryFuncs[type] = factory;
            return factory();
        }


        public IRoom CreateRoom(int type, Player master, string roomName, byte size){
            if (RoomAutoIncID == int.MaxValue - 1) {
                RoomAutoIncID = 0;
            }

            var id = ++RoomAutoIncID;

            IRoom room = CreateRoom(type);
            if (room == null) {
                Debug.LogError($"Can not load game DLL type = {type} roomName = {roomName}");
                return null;
            }

            Debug.Log($"CreateRoom type = {type} name = {roomName}");
            roomId2Room.Add(id, room);
            _allRooms.Add(room);
            if (gameId2Rooms.TryGetValue(type, out var roomLst)) {
                roomLst.Add(room);
            }
            else {
                var lst = new List<IRoom>();
                lst.Add(room);
                gameId2Rooms.Add(type, lst);
            }

            room.DoStart(type, id, this, size, roomName);
            room.OnPlayerEnter(master);
            SendCreateRoomResult(master);
            return room;
        }


        private void SendCreateRoomResult(Player player){
            var writer = new Serializer();
            writer.Put((byte) EMsgCL.L2C_RoomMsg);
            new Msg_CreateRoomResult() {ip = "127.0.0.1", port = _udpPort, roomId = player.RoomId}.Serialize(writer);
            var bytes = Compressor.Compress(writer);
            player.SendLobby(bytes);
        }

        public bool JoinRoom(long playerID, int roomID){
            var player = GetPlayer(playerID);
            if (player == null) {
                Debug.LogError($"null player  {playerID} join room {roomID} ");
                return false;
            }

            var room = GetRoom(roomID);
            if (room == null) {
                Debug.LogError($"player{playerID} try to enter a room which not exist {roomID} ");
                return false;
            }

            if (player.status != EPlayerStatus.Idle) {
                Debug.LogError($"player status {player.status} can not sit down");
                return false;
            }

            if (player.room != null) {
                Debug.LogError($"player  {playerID} already in room, should leave the room first");
                return false;
            }

            room.OnPlayerEnter(player);
            return true;
        }

        public bool LeaveRoom(long playerID){
            var player = GetPlayer(playerID);
            if (player == null) {
                Debug.LogError($"null player  {playerID} leave room ");
                return false;
            }

            var room = player.room;
            if (room == null) {
                Debug.LogError($"player {playerID} not in room, can not leave");
                return false;
            }

            player.room.OnPlayerLeave(player);
            player.room = null;
            player.status = EPlayerStatus.Idle;
            return true;
        }

        #endregion

        #region player

        public void TickOut(Player player, int reason){
            Debug.LogError($"TickPlayer reason:{reason} {player.ToString()}");
            player.lobbySock.Disconnect();
        }

        public Player GetPlayer(long playerId){
            return playerID2Player.GetRefVal(playerId);
        }

        public Player GetPlayer(int netID){
            return netID2Player.GetRefVal(netID);
        }

        public Player GetPlayerRoom(int netID){
            return netID2Player.GetRefVal(netID);
        }

        public Player AddPlayer(int netID){
            if (PlayerAutoIncID >= long.MaxValue - 1) {
                PlayerAutoIncID = 1;
            }

            var playerID = PlayerAutoIncID++;
            return CreatePlayer(playerID, netID);
        }

        public void RemovePlayer(int netID){
            if (netId2NetPeer.ContainsKey(netID)) {
                netId2NetPeer.Remove(netID);
                if (netID2Player.TryGetValue(netID, out var player)) {
                    netID2Player.Remove(netID);
                    playerID2Player.Remove(player.PlayerId);
                }
            }
        }

        public Player CreatePlayer(long playerID, int netID){
            var player = new Player();
            player.PlayerId = playerID;
            player.netID = netID;
            player.lobbySock = netId2NetPeer[netID];
            netID2Player[netID] = player;
            playerID2Player[playerID] = player;
            return player;
        }

        #endregion

        #region Conn status

        //Net infos
        public void OnClientConnected(object objPeer){
            var peer = (NetPeer) objPeer;
            Debug.Log($"OnClientConnected netID = {peer.Id}");
            netId2NetPeer[peer.Id] = peer;
        }

        public void OnCilentDisconnected(object objPeer){
            var peer = (NetPeer) objPeer;
            Debug.Log($"OnCilentDisconnected netID = {peer.Id}");
            var player = GetPlayer(peer.Id);
            if (player != null) {
                LeaveRoom(player.PlayerId);
                RemovePlayer(peer.Id);
            }
        }

        #endregion

        #region Msg Handler

        public void OnDataReceived(NetPeer peer, byte[] data){
            int netID = peer.Id;
            try {
                realOnDataReceived(netID, data);
            }
            catch (Exception e) {
                Debug.LogError($"netID{netID} parse msg Error:{e.ToString()}");
            }
        }

        public void realOnDataReceived(int netID, byte[] data){
            var reader = new Deserializer(Compressor.Decompress(data));
            var msgType = reader.GetByte();
            var playerID = reader.GetLong();
            if (msgType >= MAX_HANDLER_IDX) {
                Debug.LogError("msgType outof range");
                return;
            }

            //Debug.Log($"OnDataReceived netID = {netID}  type:{(EMsgCL)msgType}");
            {
                if (CheckMsg(reader, netID, playerID, msgType == (byte) EMsgCL.C2L_InitMsg, out var player)) return;
                var _func = allMsgDealFuncs[msgType];
                if (_func != null) {
                    _func(player, reader);
                }
                else {
                    Debug.LogError("ErrorMsg type :no msgHnadler" + msgType);
                }
            }
        }

        private void RegisterMsgHandlers(){
            RegisterNetMsgHandler(EMsgCL.C2L_InitMsg, OnMsg_InitMsg);
            RegisterNetMsgHandler(EMsgCL.C2L_JoinRoom, OnMsg_JoinRoom);
            RegisterNetMsgHandler(EMsgCL.C2L_CreateRoom, OnMsg_CreateRoom);
            RegisterNetMsgHandler(EMsgCL.C2L_LeaveRoom, OnMsg_LeaveRoom);
            RegisterNetMsgHandler(EMsgCL.C2L_RoomMsg, OnMsg_RoomMsg);
        }

        private void RegisterNetMsgHandler(EMsgCL type, DealNetMsg func){
            allMsgDealFuncs[(int) type] = func;
        }

        private bool CheckMsg(Deserializer reader, int netID, long playerID, bool isInit, out Player player){
            if (isInit) { //初始化信息处理
                var isReconn = playerID > 0;
                player = GetPlayer(playerID);
                if (isReconn) {
                    if (player == null) {
                        player = CreatePlayer(playerID, netID);
                    }
                }
                else {
                    player = AddPlayer(netID);
                }
            }
            else {
                player = GetPlayer(playerID);
            }

            if (player == null) {
                Debug.LogError($"ErrorMsg: have no player {playerID}");
                return true;
            }

            if (player.netID != netID) {
                Debug.LogError($"ErrorMsg: netID error: faker? netID{netID}!= player.netID = {player.netID}");
                return true;
            }

            return false;
        }

        private void OnMsg_InitMsg(Player player, Deserializer reader){
            var initMsg = reader.Parse<Msg_InitMsg>();
            player.name = initMsg.name;
            SendReqInit(player);
        }

        private void SendReqInit(Player player){
            var writer = new Serializer();
            writer.Put((byte) EMsgCL.L2C_ReqInit);
            new Msg_ReqInit() {playerId = player.PlayerId}.Serialize(writer);
            var bytes = Compressor.Compress(writer);
            player.SendLobby(bytes);
        }


        private void OnMsg_CreateRoom(Player player, Deserializer reader){
            var msg = reader.Parse<Msg_CreateRoom>();
            if (_allRooms.Count > 0) {
                JoinRoom(player.PlayerId, _allRooms[0].RoomId);
                SendCreateRoomResult(player);
            }
            else {
                CreateRoom(msg.type, player, msg.name, msg.size);
            }
        }

        private void OnMsg_LeaveRoom(Player player, Deserializer reader){ }
        private void OnMsg_JoinRoom(Player player, Deserializer reader){ }

        private void OnMsg_RoomMsg(Player player, Deserializer reader){
            var room = player.room;
            if (room == null) {
                Debug.LogError($"MsgError:Player {player.PlayerId} not in room");
                return;
            }

            room.OnRecvMsg(player, reader);
        }

        #endregion
    }
}