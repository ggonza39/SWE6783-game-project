using UnityEngine;

[DefaultExecutionOrder(250)]
public sealed class UXFacilityCameraFollow : MonoBehaviour
{
    private Transform player;
    private Camera view;
    private float halfWorldWidth=14.5f,halfWorldHeight=8.5f;
    private void Start()
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
        view = GetComponent<Camera>();
        UXMissionDefinition definition=FindFirstObjectByType<UXMissionDefinition>();
        if(definition!=null){halfWorldWidth=definition.halfWidth;halfWorldHeight=definition.halfHeight;}
        if (player != null) transform.position = Clamped(player.position);
    }
    private Vector3 Clamped(Vector3 at)
    {
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * view.aspect;
        return new Vector3(halfWidth >= halfWorldWidth ? 0f : Mathf.Clamp(at.x, -halfWorldWidth + halfWidth, halfWorldWidth - halfWidth),
            halfHeight >= halfWorldHeight ? 0f : Mathf.Clamp(at.y, -halfWorldHeight + halfHeight, halfWorldHeight - halfHeight), -10f);
    }
    private void LateUpdate()
    {
        if (player == null || view == null || Time.timeScale <= 0f) return;
        transform.position = Vector3.Lerp(transform.position, Clamped(player.position),
            1f - Mathf.Exp(-6f * Time.deltaTime));
    }
}
