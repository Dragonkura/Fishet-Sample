using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : NetworkBehaviour
{
    public Transform playerContainerUI;
    public PlayerUI playerPrefab;
    public List<PlayerUI> playerUIs = new();
    public GameObject startButton;
    // Update is called once per frame
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        startButton.SetActive(IsServer);
    }
    [Server]
    public void OnStartButtonClick()
    {
        GamePlayCore.instance.isGameStart = true;
        startButton.gameObject.SetActive(false);
    }
    public void UpdatePlayerInfo(int cID, PlayerInfo playerInfo)
    {
        var playerUI = playerUIs.Find(x => x.cID == cID);
        if(playerUI != null)
        {
            playerUI.playerScore.text = playerInfo.score.ToString();
        }
    }
    public void AddPlayerInfo(int cID, PlayerInfo playerInfo)
    {
        var playerUi = Instantiate(playerPrefab, playerContainerUI);
        playerUi.playerName.text = playerInfo.playerName;
        playerUi.playerScore.text = playerInfo.score.ToString();
        playerUi.cID = cID;
        playerUi.ownerBanner.SetActive(LocalConnection.ClientId == cID);
        playerUIs.Add(playerUi);
    }
    public void RemovePlayerInfo(int cID, PlayerInfo playerInfo)
    {
        var playerUI = playerUIs.Find(x => x.cID == cID);
        if (playerUI != null)
        {
            playerUIs.Remove(playerUI);
            Destroy(playerUI.gameObject);
        }
    }
    public void ClearAll()
    {
        for (int i = 0; i < playerUIs.Count; i++)
        {
            var item = playerUIs[i];
            playerUIs.Remove(item);
            i--;
            Destroy(item.gameObject);
        }
    }
    public void InitUI(Dictionary<int, PlayerInfo> playerDic)
    {
        ClearAll();
        foreach (var item in playerDic)
        {
            AddPlayerInfo(item.Key, new PlayerInfo() { playerName = item.Value.playerName, score = item.Value.score });
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            OnMoveButtonClick(1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            OnMoveButtonClick(2);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnMoveButtonClick(3);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnMoveButtonClick(4);
        }
    }
    public void OnMoveButtonClick(int dir)
    {
        if (IsServer) return;
        if (!GamePlayCore.instance.isGameStart) return;
        var player = LocalConnection.FirstObject.GetComponent<Player>();
        player.MoveToDirection(dir);
    }
}
