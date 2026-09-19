using UnityEngine;
namespace BeforeJericho
{
    public sealed class ShoulderCamera : MonoBehaviour
    {
        public HunterMotor player; public HuntSession session; public Camera lens;
        public float sensitivity = .11f; public float Yaw { get; private set; }
        float _pitch = 12, _pivotHeight = 1.55f;
        Renderer[] _bodyRenderers; bool _bodyHidden;
        void Start() { if(player && player.body) _bodyRenderers = player.body.GetComponentsInChildren<Renderer>(true); }
        void OnDisable() { if(_bodyRenderers != null) foreach(var renderer in _bodyRenderers) if(renderer) renderer.forceRenderingOff = false; }
        public void Face(float yaw, float pitch = 12) { Yaw = yaw; _pitch = pitch; }
        void LateUpdate()
        {
            if(!player || !player.Input) return;
            bool aiming = session.Playing && player.CanAim && player.Input.Aim;
            if(session.Playing)
            {
                Vector2 delta = player.Input.Look * sensitivity * (aiming ? .65f : 1);
                Yaw += delta.x; _pitch = Mathf.Clamp(_pitch - delta.y, -40, 65);
            }
            Quaternion rotation = Quaternion.Euler(_pitch, Yaw, 0);
            _pivotHeight = Mathf.Lerp(_pivotHeight, player.CameraHeight, 12 * Time.unscaledDeltaTime);
            Vector3 pivot = player.transform.position + Vector3.up * _pivotHeight;
            Vector3 offset = rotation * new Vector3(.6f, .1f, aiming ? -1.55f : -3.1f);
            float distance = offset.magnitude;
            // Player uses Ignore Raycast; scene geometry remains on Default.
            if(Physics.SphereCast(pivot, .18f, offset.normalized, out RaycastHit hit, distance, 1, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(.15f, hit.distance - .08f);
            transform.SetPositionAndRotation(pivot + offset.normalized * distance, rotation);
            // A wall-compressed camera must not fill the view with the player mesh.
            bool hideBody = distance < (_bodyHidden ? 1.15f : .95f);
            if(hideBody != _bodyHidden && _bodyRenderers != null)
            { _bodyHidden = hideBody; foreach(var renderer in _bodyRenderers) if(renderer) renderer.forceRenderingOff = hideBody; }
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, aiming ? 52 : 68, 12 * Time.unscaledDeltaTime);
        }
    }
}
