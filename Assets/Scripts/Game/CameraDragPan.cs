using UnityEngine;
using UnityEngine.EventSystems;

namespace Universes.Game
{
    [RequireComponent(typeof(Camera))]
    public class CameraDragPan : MonoBehaviour
    {
        [SerializeField] private bool dragEnabled = true;
        [SerializeField] private int dragMouseButton = 1;
        [SerializeField] private float dragSpeed = 1f;
        [SerializeField] private bool zoomEnabled = true;
        [SerializeField] private float zoomSpeed = 2f;

        private Camera _camera;
        private Vector3 _dragOrigin;
        private bool _dragging;
        private GameController _gameController;
        private bool _useDynamicZoomLimits;

        public void Configure(bool enabled, int mouseButton, float speed)
        {
            dragEnabled = enabled;
            dragMouseButton = Mathf.Clamp(mouseButton, 0, 2);
            dragSpeed = Mathf.Max(0f, speed);
        }

        public void ConfigureZoom(bool enabled, float speed)
        {
            zoomEnabled = enabled;
            zoomSpeed = Mathf.Max(0f, speed);
            _useDynamicZoomLimits = true;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            if (_camera == null || !_camera.orthographic)
                return;

            HandleZoom();
            HandleDrag();
        }

        private void LateUpdate()
        {
            if (!_useDynamicZoomLimits || _camera == null || !_camera.orthographic)
                return;

            ClampZoomToPhaseLimits();
        }

        private void ClampZoomToPhaseLimits()
        {
            if (!_useDynamicZoomLimits || !TryGetDynamicZoomLimits(out var minSize, out var maxSize))
                return;

            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minSize, maxSize);
        }

        private void HandleZoom()
        {
            if (!zoomEnabled || zoomSpeed <= 0f)
                return;

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            if (IsPointerOverUi())
                return;

            ResolveZoomLimits(out var minSize, out var maxSize);
            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize - scroll * zoomSpeed,
                minSize,
                maxSize);

            if (_gameController == null)
                _gameController = FindAnyObjectByType<GameController>();

            _gameController?.CancelCameraZoomAnimation();
        }

        private void HandleDrag()
        {
            if (!dragEnabled || dragSpeed <= 0f)
                return;

            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                if (IsPointerOverUi())
                    return;

                if (dragMouseButton == 1 && StarView.GetStarUnderMouse() != null)
                    return;

                _dragOrigin = GetMouseWorldPosition();
                _dragging = true;
            }

            if (Input.GetMouseButtonUp(dragMouseButton))
                _dragging = false;

            if (!_dragging || !Input.GetMouseButton(dragMouseButton))
                return;

            var difference = _dragOrigin - GetMouseWorldPosition();
            transform.position += difference * dragSpeed;
        }

        private void ResolveZoomLimits(out float minSize, out float maxSize)
        {
            if (_useDynamicZoomLimits && TryGetDynamicZoomLimits(out minSize, out maxSize))
                return;

            minSize = 3f;
            maxSize = 15f;
        }

        private bool TryGetDynamicZoomLimits(out float minSize, out float maxSize)
        {
            minSize = 3f;
            maxSize = 15f;

            if (_gameController == null)
                _gameController = FindAnyObjectByType<GameController>();

            return CameraZoomLimits.TryGetForPhase(_gameController, out minSize, out maxSize);
        }

        private Vector3 GetMouseWorldPosition()
        {
            var depth = Mathf.Abs(transform.position.z);
            var world = _camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));
            world.z = transform.position.z;
            return world;
        }

        private static bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
