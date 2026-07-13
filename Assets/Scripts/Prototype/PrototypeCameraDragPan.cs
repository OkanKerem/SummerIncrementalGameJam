using UnityEngine;
using UnityEngine.EventSystems;

namespace Universes.Prototype
{
    [RequireComponent(typeof(Camera))]
    public class PrototypeCameraDragPan : MonoBehaviour
    {
        [SerializeField] private bool dragEnabled = true;
        [SerializeField] private int dragMouseButton = 1;
        [SerializeField] private float dragSpeed = 1f;

        private Camera _camera;
        private Vector3 _dragOrigin;
        private bool _dragging;

        public void Configure(bool enabled, int mouseButton, float speed)
        {
            dragEnabled = enabled;
            dragMouseButton = Mathf.Clamp(mouseButton, 0, 2);
            dragSpeed = Mathf.Max(0f, speed);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (!dragEnabled || dragSpeed <= 0f)
                return;

            if (_camera == null)
                _camera = GetComponent<Camera>();

            if (_camera == null || !_camera.orthographic)
                return;

            if (Input.GetMouseButtonDown(dragMouseButton))
            {
                if (IsPointerOverUi())
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
