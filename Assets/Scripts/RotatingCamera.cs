using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RotatingCamera : MonoBehaviour
{
    public float initialDistance = 10f;
    public float zoomSpeed = 10f;
    public float zoomKeySpeed = 0.01f; // Speed for zooming with keys
    public float zoomSmoothTime = 0.2f;
    public float rotationSpeed = 100f;
    public bool invertY = false; // Invert Y-axis for mouse movement
    public float mouseSensitivity = 0.3f;
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
            if (Level.selectedPlayer == null)
            {
                return target;
            }
            target = Level.selectedPlayer.transform.position;
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
