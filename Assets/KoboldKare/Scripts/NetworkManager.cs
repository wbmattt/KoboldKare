﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Components;
using TMPro;
using System;
using KoboldKare;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[CreateAssetMenu(fileName = "NewNetworkManager", menuName = "Data/NetworkManager", order = 1)]
public class NetworkManager : SingletonScriptableObject<NetworkManager>, IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, ILobbyCallbacks, IWebRpcCallback, IErrorInfoCallback {
    public bool online {
        get {
            return PhotonNetwork.OfflineMode != true && PhotonNetwork.PlayerList.Length > 1;
        }
    }
    public bool offline {
        get {
            return !online;
        }
    }
    [NonSerialized]
    private List<Transform> spawnPoints = new List<Transform>();
    public GameEvent SpawnEvent;
    public IEnumerator JoinLobbyRoutine() {
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady || (PhotonNetwork.IsConnected && PhotonNetwork.InRoom));
        if (!PhotonNetwork.InLobby) {
            PhotonNetwork.JoinLobby();
        }
    }
    public void JoinLobby() {
        // Don't load the lobby if we're playing offline.
        if (SceneManager.GetActiveScene().name == "MainMap" && PhotonNetwork.OfflineMode == true) {
            return;
        }
        GameManager.instance.StartCoroutine(JoinLobbyRoutine());
    }
    public void QuickMatch() {
        GameManager.instance.StartCoroutine(QuickMatchRoutine());
    }
    public IEnumerator QuickMatchRoutine() {
        PopupHandler.instance.SpawnPopup("Connect");
        yield return GameManager.instance.StartCoroutine(EnsureOnlineAndReadyToLoad());
        PhotonNetwork.JoinRandomRoom();
    }
    public void CreatePublicRoom() {
        GameManager.instance.StartCoroutine(CreatePublicRoomRoutine());
    }
    public IEnumerator CreatePublicRoomRoutine() {
        PopupHandler.instance.SpawnPopup("Connect");
        yield return GameManager.instance.StartCoroutine(EnsureOnlineAndReadyToLoad());
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 8 });
    }
    public void JoinMatch(string roomName) {
        GameManager.instance.StartCoroutine(JoinMatchRoutine(roomName));
    }
    public IEnumerator JoinMatchRoutine(string roomName) {
        PopupHandler.instance.SpawnPopup("Connect");
        yield return GameManager.instance.StartCoroutine(EnsureOnlineAndReadyToLoad());
        PhotonNetwork.JoinRoom(roomName);
    }
    public IEnumerator EnsureOfflineAndReadyToLoad() {
        PhotonPeer.RegisterType(typeof(ReagentContents), (byte)'R', ReagentContents.SerializeReagentContents, ReagentContents.DeserializeReagentContents);
        if (PhotonNetwork.InRoom) {
            PhotonNetwork.LeaveRoom();
            yield return LevelLoader.instance.LoadLevel("ErrorScene");
        }
        if (PhotonNetwork.InLobby) {
            PhotonNetwork.LeaveLobby();
        }
        PhotonNetwork.Disconnect();
        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        PhotonNetwork.OfflineMode = true;
    }
    public IEnumerator EnsureOnlineAndReadyToLoad(bool shouldLeaveRoom = true) {
        if (PhotonNetwork.InRoom && shouldLeaveRoom) {
            PhotonNetwork.LeaveRoom();
            yield return LevelLoader.instance.LoadLevel("ErrorScene");
        }
        PhotonNetwork.OfflineMode = false;
        PhotonPeer.RegisterType(typeof(ReagentContents), (byte)'R', ReagentContents.SerializeReagentContents, ReagentContents.DeserializeReagentContents);
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
    }
    public void StartSinglePlayer() {
        GameManager.instance.StartCoroutine(SinglePlayerRoutine());
    }
    public IEnumerator SinglePlayerRoutine() {
        yield return GameManager.instance.StartCoroutine(EnsureOfflineAndReadyToLoad());
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.JoinRandomRoom();
        yield return new WaitUntil(() => SceneManager.GetSceneByName("MainMap").isLoaded);
    }

    public void LeaveLobby() {
        if (PhotonNetwork.InLobby) {
            PhotonNetwork.LeaveLobby();
        }
    }
    public void OnEnable() {
        // GameManager actually adds us to the callbacks, PhotonNetwork doesn't initialize early enough to call it from here.
        //PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnDisable() {
        //PhotonNetwork.RemoveCallbackTarget(this);
    }
    public void OnConnectedToMaster() {
        Debug.Log("OnConnectedToMaster() was called by PUN.");
        Debug.Log("Using version " + PhotonNetwork.NetworkingClient.AppVersion);
    }
    public void OnDisconnected(DisconnectCause cause) {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
        GameManager.instance?.StartCoroutine(OnDisconnectRoutine(cause));
    }
    public IEnumerator OnDisconnectRoutine(DisconnectCause cause) {
        if (cause != DisconnectCause.DisconnectByClientLogic && cause != DisconnectCause.None) {
            yield return GameManager.instance.StartCoroutine(EnsureOnlineAndReadyToLoad());
        }
        PopupHandler.instance.SpawnPopup("Disconnect", true, cause.ToString());
    }

    public IEnumerator OnJoinRoomFailedRoutine(short returnCode, string message) {
        yield return GameManager.instance.StartCoroutine(EnsureOnlineAndReadyToLoad());
        PopupHandler.instance.SpawnPopup("Disconnect", true, "Error " + returnCode + ": " + message);
    }

    public void OnJoinRoomFailed(short returnCode, string message) {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRoomFailed() was called by PUN." + message);
        GameManager.instance.StartCoroutine(OnJoinRoomFailedRoutine(returnCode, message));
        //GameManager.instance.LoadLevel("ErrorScene");
        //PhotonNetwork.CreateRoom("asdfasdfasdfasdfasdfasdf", new RoomOptions{MaxPlayers = maxPlayersPerRoom});
        //PhotonNetwork.CreateRoom(null, new RoomOptions{MaxPlayers = maxPlayers});
    }
    public void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.");
        GameManager.instance.StartCoroutine(CreatePublicRoomRoutine());
        //if (popup != null) {
        //popup.Hide();
        //}
    }
    public IEnumerator SpawnControllablePlayerRoutine() {
        yield return new WaitUntil(() => !LevelLoader.loadingLevel);
        // If our kobold exists, don't spawn another
        if (PhotonNetwork.LocalPlayer.TagObject != null && (PhotonNetwork.LocalPlayer.TagObject as Kobold) != null) {
            yield break;
        }
        //yield return new WaitUntil(()=>(!GameManager.instance.loadingLevel && !GameManager.instance.networkManager.loading));
        for (int i = 0; i < spawnPoints.Count; i++) {
            if (spawnPoints[i] == null) {
                spawnPoints.RemoveAt(i--);
            }
        }
        if (spawnPoints.Count == 0) {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("PlayerSpawn")) {
                spawnPoints.Add(g.transform);
            }
        }
        //GameObject[] spawns = GameObject.FindGameObjectsWithTag("PlayerSpawn");
        Vector3 pos = Vector3.zero;
        if (spawnPoints.Count > 0) {
            pos = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count - 1)].position;
        }
        GameObject player = PhotonNetwork.Instantiate("GrabbableKobold4", pos, Quaternion.identity, 0, new object[] {true});
        player.GetComponentInChildren<PlayerPossession>(true).gameObject.SetActive(true);
        SpawnEvent.Raise();
        PopupHandler.instance.ClearAllPopups();
    }
    public void SpawnControllablePlayer() {
        GameManager.instance.StartCoroutine(SpawnControllablePlayerRoutine());
    }
    void IMatchmakingCallbacks.OnJoinedRoom() {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
        SpawnControllablePlayer();
        //if (popup != null) {
        //popup.Hide();
        //}
        //localPlayerInstance = GameObject.Instantiate(saveLibrary.GetPrefab(ScriptableSaveLibrary.SaveID.Kobold), Vector3.zero, Quaternion.identity);
        //localPlayerInstance.GetComponent<ISavable>().SpawnOverNetwork();
    }
    public void OnPlayerEnteredRoom(Player other) {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting
        if (PhotonNetwork.IsMasterClient) {
            //RPCSetMoney(money.data);
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
            DayNightCycle.instance?.ForceUpdate();
        }
    }

    public void OnPlayerLeftRoom(Player other) {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects
        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
        }
    }
    public void OnConnected() {
        Debug.Log("Connected.");
    }

    public void OnCreatedRoom() {
        if (SceneManager.GetActiveScene().name != "MainMap" && SceneManager.GetActiveScene().name != "MainMapRedo") {
            LevelLoader.instance.LoadLevel("MainMap");
        }
    }

    public void OnLeftRoom() {
        Debug.Log("Left room");
    }
    public void OnMasterClientSwitched(Player newMasterClient) {
        Debug.Log("Master switched!" + newMasterClient);
    }

    public void OnJoinedLobby() {
        Debug.Log("Joined lobby");
    }

    public void OnLeftLobby() {
        Debug.Log("Left lobby i guess");
    }

    public void OnRegionListReceived(RegionHandler regionHandler) {
        Debug.Log("We recieved a list i guess:" + regionHandler);
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList) {
        foreach (RoomInfo i in roomList) {
            Debug.Log("Got room info list:" + i);
        }
    }
    public void OnFriendListUpdate(List<FriendInfo> friendList) {
        Debug.Log("Friends update:" + friendList);
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) {
        Debug.Log("Custom auth i guess" + data);
    }

    public void OnCustomAuthenticationFailed(string debugMessage) {
        Debug.Log("Custom auth failed" + debugMessage);
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) {
        Debug.Log("lobby update " + lobbyStatistics);
    }

    public void OnErrorInfo(ErrorInfo errorInfo) {
        Debug.Log("Photon error: " + errorInfo);
    }

    public void OnCreateRoomFailed(short returnCode, string message) {
    }

    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) {
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) {
        //if (playerList.ContainsKey(targetPlayer.ActorNumber)) {
        //if (playerList[targetPlayer.ActorNumber] != null) {
        //playerList[targetPlayer.ActorNumber].GetComponent<Kobold>().Load(changedProps);
        //}
        //}
    }
    public void OnWebRpcResponse(OperationResponse response) {
    }
}
