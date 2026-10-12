using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Tables
{
    public sealed class OverlayPanelsView : MonoBehaviour
    {
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _helpButton;
        [SerializeField] private Button _closeSettingsButton;
        [SerializeField] private Button _closeHelpButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _helpPanel;
        [SerializeField] private TMP_Text _seriesHelp;
        [SerializeField] private TMP_Text _classicHelp;

        public void RenderHelp(bool publicMelds)
        {
            _seriesHelp.gameObject.SetActive(publicMelds);
            _classicHelp.gameObject.SetActive(!publicMelds);
        }
        private void ToggleSettings()
        {
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
            _helpPanel.SetActive(false);
        }
        private void ToggleHelp()
        {
            _helpPanel.SetActive(!_helpPanel.activeSelf);
            _settingsPanel.SetActive(false);
        }
        private void ClosePanels()
        {
            _settingsPanel.SetActive(false);
            _helpPanel.SetActive(false);
        }
        private void OnEnable()
        {
            _settingsButton.onClick.AddListener(ToggleSettings);
            _helpButton.onClick.AddListener(ToggleHelp);
            _closeSettingsButton.onClick.AddListener(ClosePanels);
            _closeHelpButton.onClick.AddListener(ClosePanels);
        }
        private void OnDisable()
        {
            _settingsButton.onClick.RemoveListener(ToggleSettings);
            _helpButton.onClick.RemoveListener(ToggleHelp);
            _closeSettingsButton.onClick.RemoveListener(ClosePanels);
            _closeHelpButton.onClick.RemoveListener(ClosePanels);
        }
    }
}
