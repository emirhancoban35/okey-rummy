using UnityEngine;
namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Scene Navigation Settings")]
    public sealed class SceneNavigationSettings : ScriptableObject
    {
        [SerializeField] private string _menuScene;
        [SerializeField] private string _tableScene;
        public string MenuScene => _menuScene;
        public string TableScene => _tableScene;
    }
}
