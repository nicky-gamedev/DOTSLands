using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DOTSNET;
using System.Linq;
using Unity.Mathematics;
using Unity.Physics;
using System.Data;
using ModunGames;

public class CanvasDebugger : MonoBehaviour
{
    Dictionary<ulong, int> terrainBelongList;
    NetworkClientSystem client;
    [SerializeField] Text displayText, troopText;
    Entity canvasEntity;
    EntityManager em;
    [SerializeField] int troopSelected;
    [SerializeField] RectTransform selectionBox;
    [SerializeField] GameObject button, button2;
    [SerializeField] Slider slider;
    CameraController controller;

    [SerializeField] ScreenGuard screenGuard;
    Vector2 startPos;

    private void Awake()
    {
        controller = Camera.main.gameObject.GetComponent<CameraController>();
        terrainBelongList = new Dictionary<ulong, int>();
        canvasEntity = Bootstrap.ClientWorld.EntityManager.CreateEntity();
        em = Bootstrap.ClientWorld.EntityManager;
        Application.targetFrameRate = -1;
        screenGuard = new ScreenGuard();
        slider.value = controller.VerticalTranslationScrollZoomSpeed;
    }

    void Update()
    {
        //Security measures
        screenGuard.Update();
        if (client == null) client = Bootstrap.ClientWorld.GetExistingSystem<NetworkClientSystem>();
        if (client.state == ClientState.DISCONNECTED)
        {
            canvasEntity = Entity.Null;
            terrainBelongList.Clear();
            return;
        }
        if (canvasEntity == Entity.Null)
        {
            canvasEntity = Bootstrap.ClientWorld.EntityManager.CreateEntity();
        }
        if (!em.HasComponent<CanvasTag>(canvasEntity))
        {
            em.AddComponentData(canvasEntity, new CanvasTag { });
        }
        var playerArray = Bootstrap.ClientWorld.GetExistingSystem<NetworkClientSystem>().spawned;


        controller.VerticalTranslationScrollZoomSpeed = slider.value;

        //Adding all the terrains on a list
        foreach (KeyValuePair<ulong, Entity> kvp in playerArray)
        {
            if (!em.HasComponent<ClaimComponent>(kvp.Value)) continue;
            var cc = em.GetComponentData<ClaimComponent>(kvp.Value);

            if (cc.belongsTo == 0) continue;

            if (terrainBelongList.ContainsKey(cc.belongsTo))
            {
                terrainBelongList[cc.belongsTo]++;
            }
            else
            {
                terrainBelongList.Add(cc.belongsTo, 1);
            }
        }

        string text = "";
        //Displaying on the screen
        foreach (KeyValuePair<ulong, int> kvp in terrainBelongList.ToList())
        {
            text += "Player " + kvp.Key + " Has " + kvp.Value + " Terrains";
            text += "\n\r";
            terrainBelongList[kvp.Key] = 0;
        }
        displayText.text = text;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        //TODO: This isn't the smartest way to do this, but putting a tag isn't working
        if (current == button || current == button2 || current == slider.gameObject) return;

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseSelectionBox();
        }

        // mouse held down
        if (Input.GetMouseButton(0))
        {
            UpdateSelectionBox(Input.mousePosition);
        }
    }

    public void AddSoldier()
    {
        em.AddComponent(canvasEntity, typeof(RequestSoldier));
    }

    public void DeleteSoldier()
    {
        em.AddComponent(canvasEntity, typeof(DeleteSelectedEntity));
    }

    void UpdateSelectionBox(Vector2 curMousePos)
    {
        if (!selectionBox.gameObject.activeInHierarchy)
            selectionBox.gameObject.SetActive(true);

        float width = curMousePos.x - startPos.x;
        float height = curMousePos.y - startPos.y;

        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBox.anchoredPosition = startPos + new Vector2(width / 2, height / 2);

        if (em.HasComponent<SelectionBox>(canvasEntity))
        {
            em.SetComponentData(canvasEntity, new SelectionBox
            {
                min = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2),
                max = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2),
                released = false
            });
        }
        else em.AddComponentData(canvasEntity, new SelectionBox
        {
            min = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2),
            max = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2),
            released = false
        });
    }

    void ReleaseSelectionBox()
    {
        //Defining min and max values from the rect
        Vector2 min = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2);
        Vector2 max = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2);

        selectionBox.gameObject.SetActive(false);

        //Passing values
        if (em.HasComponent<SelectionBox>(canvasEntity))
        {
            em.SetComponentData(canvasEntity, new SelectionBox
            {
                min = min,
                max = max,
                released = true
            });
        }
        else em.AddComponentData(canvasEntity, new SelectionBox
        {
            min = min,
            max = max,
            released = true
        });
    }
}

public struct SelectionBox : IComponentData 
{
    public float2 min;
    public float2 max;
    public bool released;
}

public struct CanvasTag : IComponentData { }

public struct DeleteSelectedEntity : IComponentData { }

