using UnityEngine;

/// <summary>
/// script for camera controls
/// </summary>
[RequireComponent(typeof(Camera))]
public class RotatingCamera : MonoBehaviour
{
    /// <summary>
    /// the distance from the target point when the game starts
    /// </summary>
    public float initialDistance = 10f;
    /// <summary>
    /// how fast the camera zooms in and out when using the scroll wheel
    /// </summary>
    public float zoomSpeed = 10f;
    /// <summary>
    /// how fast the camera zooms in and out when using the + and - keys (or = key for +)
    /// </summary>
    public float zoomKeySpeed = 0.01f;
    /// <summary>
    /// smooth time for zooming, smaller values will make the zoom feel more responsive but less smooth, larger values will make the zoom feel smoother but less responsive
    /// </summary>
    public float zoomSmoothTime = 0.2f;
    /// <summary>
    /// speed at which the camera rotates when using the arrow keys
    /// </summary>
    public float rotationSpeed = 100f;
    /// <summary>
    /// invert y axis in camera controls
    /// </summary>
    /// <remarks>
    /// i like having this on
    /// </remarks>
    public bool invertY = true;
    /// <summary>
    /// how sensitive the camera rotation is to mouse movement
    /// </summary>
    public float mouseSensitivity = 0.3f;
    /// <summary>
    /// The initial pitch angle, in degrees, used to set the starting orientation.
    /// </summary>
    public float initialPitch = 45f;

    public float minSize = 2f;
    public float maxSize = 50f;

    private float yaw = 0f;
    private float pitch;
    private float targetSize;
    private float zoomVelocity;

    private Camera cam;

    private Vector3 target = Vector3.zero;
    private Vector3 Target
    {
        get
        {
            target = new(0, 3, 0); // placeholder TODO: make this follow the player
            return target;
        }
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        pitch = initialPitch;
        targetSize = cam.orthographicSize;
    }

    private void Update()
    {
        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        float scroll = -Input.GetAxis("Mouse ScrollWheel");

        if (Input.GetKey(KeyCode.Minus))
        {
            scroll += zoomKeySpeed;
        }

        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Plus))
        {
            scroll -= zoomKeySpeed;
        }

        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetSize = Mathf.Clamp(targetSize + (scroll * zoomSpeed), minSize, maxSize);
        }

        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);
    }

    private void HandleRotation()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical") * (invertY ? -1 : 1);

        // Drag if middle or right click is pressed
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity * rotationSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * (invertY ? 1 : -1) * mouseSensitivity * rotationSpeed * Time.deltaTime;
        }
        else
        {
            yaw += inputX * rotationSpeed * Time.deltaTime;
            pitch -= inputY * rotationSpeed * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, 10f, 89f); // Prevent flipping
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.back * initialDistance;
        transform.SetPositionAndRotation(Target + direction, Quaternion.LookRotation(-direction, Vector3.up));
    }
}
