using UnityEngine;
namespace Okey.Presentation.Tables
{
    public sealed class TableAudioView : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private TableAudioSettings _settings;
        public void Play(TableSoundCue cue) => _settings.Play(_source, cue);
    }
}
