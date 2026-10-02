using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace BeforeJericho.UI
{
    /// <summary>Applies the non-UI options: camera sensitivity/invert/FOV, volume, display mode, brightness and motion blur.
    /// The post overrides live on a runtime-only volume, so authored profiles are never modified.
    /// Stored only (nothing to drive yet): camera shake, aim assist, and the dialogue/music/ambience volumes — the placeholder
    /// audio is a single synthesized effects source, driven by master + effects.</summary>
    public sealed class SettingsApplier : MonoBehaviour
    {
        [SerializeField] HuntSession _session;
        float _baseSensitivity = -1;
        Volume _volume; VolumeProfile _profile; ColorAdjustments _brightness; MotionBlur _blur;

        void OnEnable() { GameSettings.Changed += Apply; }
        void OnDisable() { GameSettings.Changed -= Apply; }
        void Start()
        {
            if(!_session) { Debug.LogError("SettingsApplier: HuntSession reference missing.", this); enabled = false; return; }
            _profile = ScriptableObject.CreateInstance<VolumeProfile>(); _profile.name = "Player options (runtime)";
            _brightness = _profile.Add<ColorAdjustments>(); _blur = _profile.Add<MotionBlur>();
            _volume = gameObject.AddComponent<Volume>(); _volume.isGlobal = true; _volume.priority = 50; _volume.sharedProfile = _profile;
            Apply();
        }
        void OnDestroy() { if(_profile) { foreach(var c in _profile.components) Destroy(c); Destroy(_profile); } }

        void Apply()
        {
            if(!_session || !_profile) return;
            var view = _session.player ? _session.player.view : null;
            if(view)
            {
                if(_baseSensitivity < 0) _baseSensitivity = view.sensitivity;
                view.sensitivity = _baseSensitivity * GameSettings.Range("sens") / 50f;
                view.aimSensitivityScale = .65f * GameSettings.Range("aimsens") / 40f;
                view.invertY = GameSettings.On("invert");
                view.fieldOfView = GameSettings.Range("fov"); view.aimFieldOfView = view.fieldOfView * 52f / 68f;
            }
            AudioListener.volume = GameSettings.Range("master") / 100f;
            if(_session.sound && _session.sound.TryGetComponent(out AudioSource source)) source.volume = GameSettings.Range("sfx") / 100f;
            if(!Application.isEditor) Screen.fullScreenMode = GameSettings.Choice("mode") == "full" ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            // 50 = neutral; ±2 EV across the slider.
            _brightness.postExposure.Override((GameSettings.Range("bright") - 50) / 25f);
            _blur.intensity.Override(GameSettings.On("blur") ? .5f : 0);
        }
    }
}
