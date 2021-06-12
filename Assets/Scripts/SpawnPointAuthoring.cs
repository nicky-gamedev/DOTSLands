using DOTSNET;
using UnityEngine;

public class SpawnPointAuthoring : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Bootstrap.ServerWorld.GetExistingSystem<PlayerJoinMessageSystem>().positions.Add(transform.position);
    }
}
