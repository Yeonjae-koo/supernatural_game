using UnityEngine;
namespace BeforeJericho
{
    public enum HunterStance { Standing, Crouching, Crawling }
    [RequireComponent(typeof(CharacterController), typeof(HunterInput))]
    public sealed class HunterMotor : MonoBehaviour
    {
        public HuntSession session; public ShoulderCamera view; public Transform body;
        [SerializeField] float crouchSpeed = 1.35f, crawlSpeed = .75f;
        CharacterController _controller; HunterInput _input; float _vertical, _standingHeight, _stepOffset;
        Vector3 _bodyRest, _bodyScale, _muzzleRest, _lighterRest;
        HunterCombat _combat;
        readonly Collider[] _headroom = new Collider[32];
        public HunterInput Input => _input;
        public HunterStance Stance { get; private set; }
        public bool IsCrawling => Stance == HunterStance.Crawling;
        public bool CanAim => !IsCrawling;
        public float CameraHeight => IsCrawling ? .55f : Stance == HunterStance.Crouching ? 1.05f : 1.55f;
        public float InteractionHeight => IsCrawling ? .4f : Stance == HunterStance.Crouching ? .8f : 1f;
        public bool Sprinting { get; private set; }
        void Awake()
        {
            _controller = GetComponent<CharacterController>(); _input = GetComponent<HunterInput>();
            // Slow crawl deltas must remain valid even at very high frame rates.
            _controller.minMoveDistance = 0;
            _standingHeight = _controller.height; _stepOffset = _controller.stepOffset;
            if(body) { _bodyRest = body.localPosition; _bodyScale = body.localScale; }
            else { Debug.LogError("HunterMotor requires a body transform.", this); enabled = false; }
        }
        void Start()
        {
            _combat = GetComponent<HunterCombat>();
            if(_combat)
            {
                if(_combat.muzzle) _muzzleRest = _combat.muzzle.localPosition;
                if(_combat.lighter) _lighterRest = _combat.lighter.transform.localPosition;
            }
        }
        public bool TrySetStance(HunterStance stance)
        {
            float height = stance == HunterStance.Standing ? _standingHeight : stance == HunterStance.Crouching ? 1.15f : .7f;
            if(height > _controller.height && !HasHeadroom(height)) return false;
            Stance = stance; _controller.height = height; _controller.center = Vector3.up * (height * .5f);
            _controller.stepOffset = Mathf.Min(_stepOffset, height * .3f);
            return true;
        }
        bool HasHeadroom(float height)
        {
            float radius = _controller.radius - .015f;
            Vector3 feet = transform.position;
            int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * (_controller.height - radius + .025f), feet + Vector3.up * (height - radius), radius, _headroom, ~0, QueryTriggerInteraction.Ignore);
            if(count == _headroom.Length) return false;
            for(int i = 0; i < count; i++) if(_headroom[i] && !_headroom[i].transform.IsChildOf(transform)) return false;
            return true;
        }
        public void NormalizeStance() { TrySetStance(HunterStance.Standing); _vertical = 0; Sprinting = false; }
        void Update()
        {
            Sprinting = false;
            if(!session.Playing) { _vertical = 0; return; }
            if(_input.Crawl) TrySetStance(IsCrawling ? HunterStance.Standing : HunterStance.Crawling);
            if(_input.Crouch) TrySetStance(Stance == HunterStance.Crouching ? HunterStance.Standing : HunterStance.Crouching);
            Vector2 move = _input.Move;
            bool aiming = CanAim && _input.Aim;
            bool sprintRequested = _input.Sprint && move.y > .1f && !aiming;
            // Sprint may leave a low stance only when the full standing capsule fits.
            if(sprintRequested && Stance != HunterStance.Standing) TrySetStance(HunterStance.Standing);
            Sprinting = sprintRequested && Stance == HunterStance.Standing;
            float speed = IsCrawling ? crawlSpeed : Stance == HunterStance.Crouching ? crouchSpeed : aiming ? session.definition.aimSpeed : Sprinting ? session.definition.sprintSpeed : session.definition.walkSpeed;
            if(move.y < 0) speed *= .8f;
            else if(Mathf.Abs(move.x) > Mathf.Abs(move.y)) speed *= .9f;
            Vector3 direction = Quaternion.Euler(0, view.Yaw, 0) * new Vector3(move.x, 0, move.y);
            _vertical = _controller.isGrounded ? -2 : Mathf.Max(-25, _vertical - 20 * Time.deltaTime);
            _controller.Move((direction * speed + Vector3.up * _vertical) * Time.deltaTime);
            if(direction.sqrMagnitude > .01f || aiming)
            {
                Quaternion target = Quaternion.Euler(0, aiming ? view.Yaw : Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 14 * Time.deltaTime);
            }
            float ratio = _controller.height / _standingHeight;
            body.localScale = new Vector3(_bodyScale.x, _bodyScale.y * ratio, _bodyScale.z);
            body.localPosition = _bodyRest + Vector3.up * (move.sqrMagnitude > .1f && !IsCrawling ? Mathf.Sin(Time.time * 13) * .025f : 0);
            if(_combat)
            {
                if(_combat.muzzle) { var p = _muzzleRest; p.y *= ratio; _combat.muzzle.localPosition = p; }
                if(_combat.lighter) { var p = _lighterRest; p.y *= ratio; _combat.lighter.transform.localPosition = p; }
            }
        }
        public void Teleport(Vector3 point)
        {
            _controller.enabled = false; transform.position = point; _vertical = 0;
            Stance = HunterStance.Standing; _controller.height = _standingHeight; _controller.center = Vector3.up * (_standingHeight * .5f); _controller.stepOffset = _stepOffset;
            body.localScale = _bodyScale; body.localPosition = _bodyRest; _controller.enabled = true;
        }
    }
}
