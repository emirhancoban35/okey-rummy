using System;
using UnityEngine;
namespace Okey.Presentation.Tables
{
    [CreateAssetMenu(menuName = "Okey/Presentation/Table Audio")]
    public sealed class TableAudioSettings : ScriptableObject
    {
        [Serializable]
        private struct Cue
        {
            public TableSoundCue Sound;
            public AudioClip Clip;
            [Range(0, 1)] public float Volume;
        }
        [SerializeField, Range(0, 1)] private float _volume = .6f;
        [SerializeField] private Cue[] _cues;
        public void Play(AudioSource source, TableSoundCue sound)
        {
            foreach (var cue in _cues)
                if (cue.Sound == sound)
                {
                    source.PlayOneShot(cue.Clip, cue.Volume * _volume);
                    return;
                }
        }
    }
}
