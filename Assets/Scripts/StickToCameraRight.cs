using UnityEngine;

[ExecuteAlways]
public class StickToCameraRight : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;  // твоя Main/Gameplay camera (та, что с rect 16:9)
    [SerializeField] private Vector2 viewportAnchor = new(0.98f, 0.5f); // почти справа, по центру Y
    [SerializeField] private Vector2 worldOffset = Vector2.zero;        // тонкая подстройка
    [SerializeField] private bool keepZ = true;

    private void Reset()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!targetCamera) return;

        // Для ортографической камеры Z не важен, но Unity требует корректный z относительно камеры
        float z = keepZ ? (transform.position.z - targetCamera.transform.position.z) : 0f;

        Vector3 world = targetCamera.ViewportToWorldPoint(new Vector3(viewportAnchor.x, viewportAnchor.y, z));
        world.x += worldOffset.x;
        world.y += worldOffset.y;

        if (keepZ) world.z = transform.position.z;
        transform.position = world;
    }
}
