using UnityEngine;

/// <summary>
/// idk, figure it out yourself
/// </summary>
[RequireComponent(typeof(MeshRenderer), typeof(MeshCollider))]
[System.Serializable]
public class MovementArrow : MonoBehaviour
{
    Material material;

    public Vector3Int direction;

    private bool hovering = false;

    public void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        material.EnableKeyword("_EMISSION"); // make it glow so can see it even if the lighting is bad
        material.SetColor("_EmissionColor", Color.white * 2);
    }

    public void OnMouseEnter()
    {
        material.SetColor("_BaseColor", Color.white * 2);
        transform.localScale *= 1.2f;

        hovering = true;
    }

    public void OnMouseExit()
    {
        material.SetColor("_BaseColor", Color.gray9);
        transform.localScale /= 1.2f;

        hovering = false;
    }

    private void Update()
    {
        if (hovering && Input.GetMouseButtonDown(0))
        {
            Level.Instance.OnArrowClicked(direction);
        }
    }
}
