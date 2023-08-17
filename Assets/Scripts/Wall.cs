using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    public WallPos wallPos;
    private void Start()
    {
        Vector2 spawnPosition = new();
        switch (wallPos)
        {
            case WallPos.Up:
                spawnPosition = new Vector2(0.5f, 1);
                break;
            case WallPos.Down:
                spawnPosition = new Vector2(0.5f, 0);

                break;
            case WallPos.Left:
                spawnPosition = new Vector2(0, 0.5f);

                break;
            case WallPos.Rigt:
                spawnPosition = new Vector2(1, 0.5f);

                break;
            default:
                break;
        }
         var pos =  Camera.main.ViewportToWorldPoint(spawnPosition);
        pos.z = 0;
        transform.position = pos;
    }
    public enum WallPos
    {
        Up,
        Down,
        Left,
        Rigt
    }
}
