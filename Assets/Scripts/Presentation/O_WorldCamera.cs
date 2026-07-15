using UnityEngine;

namespace Universes.Presentation
{
    [RequireComponent(typeof(Camera))]
    public class O_WorldCamera : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 0.05f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 15f;

        private Camera _camera;
        private Vector3 _dragOrigin;
        private bool _dragging;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            _camera.orthographic = true;
            _camera.orthographicSize = 8f;
        }

        private void Update()
        {
            HandleZoom();
            HandlePan();
        }

        private void HandleZoom()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize - scroll * zoomSpeed,
                minZoom,
                maxZoom);
        }

        private void HandlePan()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
                _dragging = true;
            }

            if (Input.GetMouseButtonUp(1))
                _dragging = false;

            if (!_dragging)
                return;

            var difference = _dragOrigin - (Vector3)_camera.ScreenToWorldPoint(Input.mousePosition);
            transform.position += difference * panSpeed;
        }
    }
}
