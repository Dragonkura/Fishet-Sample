using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CirclePoint : MonoBehaviour
{
    public Action<int> OnPointCollected;
    public int id;
}
