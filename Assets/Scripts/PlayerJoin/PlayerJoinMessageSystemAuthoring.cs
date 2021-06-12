using UnityEngine;
using DOTSNET;
using System;

public class PlayerJoinMessageSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(PlayerJoinMessageSystem);
}
