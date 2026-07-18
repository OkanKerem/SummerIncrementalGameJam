using System;
using UnityEngine;

namespace Universes.Game
{
    public class DestroyRocketView : MonoBehaviour
    {
        private DestroyRocketPool _pool;
        private Vector3 _start;
        private Vector3 _end;
        private float _speed;
        private float _duration;
        private float _elapsed;
        private Func<Vector3> _getEndPosition;
        private Action<Vector3> _onArrived;
        private bool _activeFlight;

        public void BindPool(DestroyRocketPool pool) => _pool = pool;

        public void Launch(Vector3 start, Vector3 end, Func<Vector3> getEndPosition, float speed,
            Action<Vector3> onArrived)
        {
            _start = start;
            _end = end;
            _getEndPosition = getEndPosition;
            _speed = Mathf.Max(0.1f, speed);
            _duration = Mathf.Max(0.1f, Vector3.Distance(start, end) / _speed);
            _elapsed = 0f;
            _onArrived = onArrived;
            _activeFlight = true;

            transform.position = _start;
            FaceTravelDirection();
            enabled = true;
        }

        public void StopAndReturnToPool()
        {
            _activeFlight = false;
            _onArrived = null;
            _getEndPosition = null;
            enabled = false;

            if (_pool != null)
                _pool.Return(this);
            else
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_activeFlight)
                return;

            _elapsed += Time.deltaTime;
            if (_getEndPosition != null)
                _end = _getEndPosition();

            var t = Mathf.Clamp01(_elapsed / _duration);
            var easedT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(_start, _end, easedT);
            FaceTravelDirection();

            if (t < 1f)
                return;

            transform.position = _end;
            _activeFlight = false;
            var callback = _onArrived;
            _onArrived = null;
            callback?.Invoke(_end);
            StopAndReturnToPool();
        }

        private void FaceTravelDirection()
        {
            var direction = _end - transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = _end - _start;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }
}
