using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using DOTSNET;
using Unity.Rendering;
using System.Collections.Generic;

public class AnimationManagement : MonoBehaviour
{
    public Animator anim;
    public Entity entityToFollow = Entity.Null;
    public Renderer render;
    public List<Material> materials;

    public bool moving;
    public float3 frwd;
    NetworkClientSystem client;

    void Update()
    {
        if (client == null)
        {
            client = Bootstrap.ClientWorld.GetExistingSystem<NetworkClientSystem>();
            return;
        }

        var em = Bootstrap.ClientWorld.EntityManager;

        if (client.state == ClientState.DISCONNECTED) Destroy(gameObject);
        if (entityToFollow == Entity.Null) return;

        AnimationInputComponent input = new AnimationInputComponent();
        Translation translation = new Translation();

        if (em.HasComponent<SelectedTroopComponent>(entityToFollow))
        {
            render.material = materials[1];
        }
        else
        {
            render.material = materials[0];
        }

        try
        {
            input = em.GetComponentData<AnimationInputComponent>(entityToFollow);
            translation = em.GetComponentData<Translation>(entityToFollow);
            moving = input.moving;
            frwd = input.forward;
            translation.Value.y -= 0.5f;
        }
        catch 
        {
            Destroy(gameObject);
        }
        

        anim.SetBool("Movement", moving);
        if (moving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(frwd);
            var rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
            transform.SetPositionAndRotation(translation.Value, rotation);
        }
        else
        {
            transform.SetPositionAndRotation(translation.Value, transform.rotation);
        }
    }
}
