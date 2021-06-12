using UnityEngine;
using DOTSNET;
using System;

public class PlayerJoinSystemAuthoring : MonoBehaviour, SelectiveSystemAuthoring
{
    public Type GetSystemType() => typeof(PlayerJoinSystem);
}
