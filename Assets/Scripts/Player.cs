using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
[Serializable]
public class SnakePoint
{
    public Vector3 Position;
    public float deltaDistance;
}

public class Player : NetworkBehaviour
{
    public Unit snakeBody;
    public Transform bodyContainer;
    [SyncObject] private readonly SyncList<SnakePoint> m_SnakePath = new SyncList<SnakePoint>();
    [SyncObject] private readonly SyncList<Unit> m_Bodies = new SyncList<Unit>();
    public Unit Head => m_Bodies[0];

    [Range(0.1f, 5)]
    private float m_BodySpace = 0.55f;

    private Rigidbody2D _rigidbody2D;
    public Vector3 moveDir = Vector2.up;
    [SerializeField] TextMeshProUGUI playerName;
    [SyncVar] float moveSpeed = 0.5f;

    [SyncVar(OnChange = nameof(LocalSetName))]public int clientId = -1;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) return;
        ServerSetId(LocalConnection.ClientId);
        Init();
    }
    [ServerRpc]
    void ServerSetId(int id)
    {
        clientId = id;
    }
    public void Init(int startLength = 3)
    {
        ServerRemoveAllBody();
        ServerAddBody(startLength);
    }
    [Client]
    private void LocalSetName(int oldValue, int newValue, bool asServer)
    {
        var name = GamePlayCore.instance.playerDic[newValue].playerName;
        playerName.text = name;
    }

    [ServerRpc]
    public void ServerAddBody(int length)
    {
        for (int i = 0; i < length; i++)
        {
            var segment = Instantiate(snakeBody, bodyContainer);
            segment.transform.localPosition = Vector3.zero;
            InstanceFinder.ServerManager.Spawn(segment.gameObject);
            m_Bodies.Add(segment);
        }
    }

    [ServerRpc]
    private void ServerRemoveAllBody()
    {
        for (int i = 0; i < m_Bodies.Count; i++)
        {
            var item = m_Bodies[i];
            m_Bodies.Remove(item);
            i--;
            Despawn(item.gameObject, DespawnType.Destroy);
        }
    }
    private void Update()
    {
        if (m_Bodies.Count == 0)
            return;
        if (GamePlayCore.instance.isGameStart )
        {
            ServerUpdateBody();
        }
    }
    [Server(Logging = FishNet.Managing.Logging.LoggingType.Off)]
    private void ServerUpdateBody()
    {
        UpdatePath();
        UpdateBodies();
    }

    [ServerRpc(RequireOwnership = true)]
    public void MoveToDirection(int dir)
    {
        switch (dir)
        {
            case 1:// UP
                moveDir = Vector2.up;
                break;
            case 2://Down
                moveDir = Vector2.down;
                break;
            case 3://Right
                moveDir = Vector2.right;
                break;
            case 4://Left
                moveDir = Vector2.left;
                break;
            default:
                break;
        }
        _rigidbody2D.velocity = moveDir * moveSpeed;
    }
    private void UpdatePath()
    {
        if (Head == null)
            return;
        var curPoint = new SnakePoint();
        curPoint.Position = Head.transform.position;
        if (m_SnakePath.Count > 0)
        {
            var lastPoint = m_SnakePath[m_SnakePath.Count - 1];
            curPoint.deltaDistance = Vector3.Distance(curPoint.Position, lastPoint.Position);
        }
        m_SnakePath.Add(curPoint);
    }
    private void UpdateBodies()
    {
        if (m_Bodies.Count <= 1)
            return;
        for (int i = 1; i < m_Bodies.Count; ++i)
        {
            float remainDistance = Mathf.Clamp(m_BodySpace, 0.1f, 5) * i;
            for (int j = m_SnakePath.Count - 1; j > 0; j--)
            {
                if (remainDistance <= m_SnakePath[j].deltaDistance)
                {
                    float LerpProgress = 0;
                    if (m_SnakePath[j].deltaDistance > 0)
                    {
                        LerpProgress = remainDistance / m_SnakePath[j].deltaDistance;
                    }
                    m_Bodies[i].transform.position = Vector3.Lerp(
                        m_SnakePath[j].Position,
                        m_SnakePath[j - 1].Position,
                        LerpProgress);
                    // Optimization 2 : remove the points before the waypoint that last body has reached
                    if (i == m_Bodies.Count - 1)
                    {
                        for (int a = 0; a < j - 1; a++)
                        {
                            m_SnakePath.Remove(m_SnakePath[a]);
                            j--;
                        }
                    }
                    // Optimization 2
                    break;
                }
                remainDistance -= m_SnakePath[j].deltaDistance;
            }
        }
    }

    void RespawnPlayer()
    {
        ServerResetPos();
        Init();
        ServerResetScore();
    }

    [ServerRpc]
    private void ServerResetPos()
    {
        transform.position = Vector3.zero;
        ClientResetPos();
    }
    [ObserversRpc]
    private void ClientResetPos()
    {
        transform.position = Vector3.zero;
    }
    [ServerRpc]
    void ServerResetScore()
    {
        var player = GamePlayCore.instance.playerDic[clientId];
        if(player != null)
        {
            player.score = 0;
            GamePlayCore.instance.playerDic.Dirty(clientId);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!base.IsOwner) return;
        if(collision.tag == "Point")
        {
            var point = collision.gameObject.GetComponent<CirclePoint>();
            point.OnPointCollected?.Invoke(point.id);
            ServerAddBody(1);
        }
        else if (collision.tag == "Obstacle")
        {
            RespawnPlayer();
        }
    }
}
