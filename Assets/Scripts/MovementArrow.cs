using UnityEngine;

/// <summary>
/// idk, figure it out yourself
/// </summary>
[RequireComponent(typeof(MeshRenderer), typeof(MeshCollider))]
[System.Serializable]
public class MovementArrow : MonoBehaviour
{
    MeshRenderer meshRenderer;

    public Vector3Int direction;

    public void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void OnMouseEnter()
    {
        meshRenderer.material.color = Color.white;
        transform.localScale.Set(1.2f, 1.2f, 1.2f);
    }

    public void OnMouseExit()
    {
        meshRenderer.material.color = Color.grey;
        transform.localScale.Set(1f, 1f, 1f);
    }

    public void OnClick()
    {
        Level.Instance.OnArrowClicked(direction);
    }
}
