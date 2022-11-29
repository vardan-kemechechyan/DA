using System.Collections;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    [SerializeField] Animator doorsAnimator;
    [SerializeField] MeshRenderer wallRenderer;

    bool isOpen;
    public bool IsOpen 
    {
        get => isOpen;
        set 
        {
            doorsAnimator.SetBool("open", value);
            isOpen = value;
        }
    }

    public void SetMaterial(Material material) 
    {
        wallRenderer.material = material;
    }
}