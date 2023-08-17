using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PointData
{
    public int id;
    public Vector3 viewportPos;
}
public class PlayerInfo
{
    public string playerName;
    public int score;
}
public class GamePlayCore : NetworkBehaviour
{
    [SerializeField] float spawnTime = 3f;
    private float counter;
    public CirclePoint circlePoint;
    public List<CirclePoint> circlePoints;
    public UIManager uIManager;
    [SyncObject] public readonly SyncDictionary<int, PlayerInfo> playerDic = new SyncDictionary<int, PlayerInfo>();
    [SyncVar] public bool isGameStart;
    public static GamePlayCore instance;
    private void Awake()
    {
        if (instance != null) return;
        else instance = this;
    }
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (base.IsServer)
        {
            isGameStart = false;
            playerDic.Clear();
            uIManager.ClearAll();
        }

        playerDic.OnChange += _players_OnChange;
    }

    private void _players_OnChange(SyncDictionaryOperation op,
    int key, PlayerInfo value, bool asServer)
    {
        /* Key will be provided for
        * Add, Remove, and Set. */
        switch (op)
        {
            //Adds key with value.
            case SyncDictionaryOperation.Add:
                uIManager.AddPlayerInfo(key, value);
                Debug.Log("SyncDictionaryOperation.Add");
                break;
            //Removes key.
            case SyncDictionaryOperation.Remove:
                uIManager.RemovePlayerInfo(key, value);
                Debug.Log("SyncDictionaryOperation.Remove");

                break;
            //Sets key to a new value.
            case SyncDictionaryOperation.Set:
                uIManager.UpdatePlayerInfo(key, value);
                Debug.Log("SyncDictionaryOperation.Set");

                break;
            //Clears the dictionary.
            case SyncDictionaryOperation.Clear:
                break;
            //Like SyncList, indicates all operations are complete.
            case SyncDictionaryOperation.Complete:
                break;
        }
    }
    int indexSpawn;
    public int GetClientID => LocalConnection.ClientId;


    [Server]
    private void Start()
    {
        counter = spawnTime;
        ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
    }

    private void ServerManager_OnRemoteConnectionState(NetworkConnection con, FishNet.Transporting.RemoteConnectionStateArgs state)
    {
        switch (state.ConnectionState)
        {
            case FishNet.Transporting.RemoteConnectionState.Stopped:
                playerDic.Remove(con.ClientId);
                break;
            case FishNet.Transporting.RemoteConnectionState.Started:
                playerDic.Add(con.ClientId, new PlayerInfo() { playerName = "Player:" + Random.Range(0, 9999), score = 0 });
                break;
            default:
                break;
        }
    }
    [Client(RequireOwnership = false)]
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        uIManager.ClearAll();

        playerDic.OnChange -= _players_OnChange;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
    }
    // Update is called once per frame
    void Update()
    {
        if (!isGameStart) return;
        counter -= Time.deltaTime;
        if (counter <= 0.01f)
        {
            counter = spawnTime;
            GeneratorPointData();
        }
    }
    [Server(Logging = FishNet.Managing.Logging.LoggingType.Off)]
    private void GeneratorPointData()
    {
        float spawnY = Random.Range(0.15f, 0.85f);
        float spawnX = Random.Range(0.15f, 0.85f);
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);
        indexSpawn++;

        LocalSpawn(new PointData() { 
            id = indexSpawn,

            viewportPos = spawnPosition 
        });
    }
    [ObserversRpc]
    void LocalSpawn(PointData data)
    {
        var spawnPos = Camera.main.ViewportToWorldPoint(data.viewportPos);
        spawnPos.z = 0;
        var point = Instantiate(circlePoint, spawnPos, Quaternion.identity);
        circlePoints.Add(point);
        point.id = data.id;
        var clientID = GetClientID;
        point.OnPointCollected += (id) => {
            ServerUpdate(clientID, id);
        };
    }
    [ServerRpc(RequireOwnership = false)]
    void ServerUpdate(int clientId, int Pointid, NetworkConnection networkConnection = null)
    {
        playerDic[clientId].score += 10;
        playerDic.Dirty(clientId);
        LocalUpdate(Pointid);
    }
    [ObserversRpc]
    private void LocalUpdate(int index)
    {
        var obj = circlePoints.Find(x => x.id == index);
        if (obj != null)
        {
            circlePoints.Remove(obj);
            Destroy(obj.gameObject);
        }
    }
}
