using UnityEngine;

/// <summary>
/// Configures orientation, drop height, and camera framing to keep the designed 16:9 area fully visible.
/// Works in edit mode to preview framing.
/// </summary>
[ExecuteAlways]
public class CameraSetup : MonoBehaviour
{
    [SerializeField] private bool forceLandscape = true;
    [SerializeField] private float padding = 0.5f;
    [SerializeField] private float targetAspectWidth = 16f;
    [SerializeField] private float targetAspectHeight = 9f;

    private void Start()
    {
        if (forceLandscape && Application.isPlaying)
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }

        if (Application.isPlaying && SC_GameVariables.Instance.dropHeight <= 0)
            SC_GameVariables.Instance.dropHeight = Mathf.Max(SC_GameVariables.Instance.rowsSize, SC_GameVariables.Instance.colsSize);

        FitCameraToBoard();
    }

    private void OnValidate()
    {
        FitCameraToBoard();
    }

    private void FitCameraToBoard()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        // Letterbox/pillarbox to maintain target aspect on any monitor.
        if (scaleHeight < 1f)
        {
            Rect rect = cam.rect;
            rect.width = 1f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1f - scaleHeight) * 0.5f;
            cam.rect = rect;
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1f;
            rect.x = (1f - scaleWidth) * 0.5f;
            rect.y = 0f;
            cam.rect = rect;
        }

        float halfHeight = (SC_GameVariables.Instance.colsSize - 1) / 2f + padding;
        float halfWidth = (SC_GameVariables.Instance.rowsSize - 1) / 2f + padding;

        // Fit designed 16:9 area; required width uses target aspect to keep edges visible.
        float requiredOrtho = Mathf.Max(halfHeight, halfWidth / targetAspect);
        cam.orthographicSize = requiredOrtho;

        cam.transform.position = new Vector3(
            (SC_GameVariables.Instance.rowsSize - 1) / 2f,
            (SC_GameVariables.Instance.colsSize - 1) / 2f,
            cam.transform.position.z);
    }
}
