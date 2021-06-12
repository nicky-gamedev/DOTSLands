using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using DOTSNET;

public class DotslandsClientSystem : NetworkClientSystem
{
    protected override void OnConnected()
    {
        base.OnConnected();
    }

    protected override void OnDisconnected()
    {
        base.OnDisconnected();
        EntityManager.DestroyEntity(GetEntityQuery(typeof(CanvasTag)).GetSingletonEntity());
        Send(new DisconnectedMessage { });
    }
}
