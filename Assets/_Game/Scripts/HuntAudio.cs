using UnityEngine;
namespace BeforeJericho
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class HuntAudio : MonoBehaviour
    {
        AudioSource _source; AudioClip _cue;
        void Awake()
        {
            _source = GetComponent<AudioSource>(); _source.spatialBlend = 0;
            float[] samples = new float[11025];
            for(int i=0;i<samples.Length;i++) samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 140 / 44100) * Mathf.Exp(-i / 1800f);
            _cue = AudioClip.Create("Original synthesized placeholder cue", samples.Length, 1, 44100, false); _cue.SetData(samples,0);
        }
        public void PlayCue(float hz, float duration, float volume) { _source.pitch = hz / 140; _source.PlayOneShot(_cue, volume); }
        void OnDestroy() { if(_cue) Destroy(_cue); }
    }
}
