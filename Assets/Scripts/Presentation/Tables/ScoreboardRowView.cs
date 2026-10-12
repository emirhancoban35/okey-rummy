using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Okey.Presentation.Tables
{
    public sealed class ScoreboardRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _opening;
        [SerializeField] private TMP_Text _penalty;
        [SerializeField] private TMP_Text _round;
        [SerializeField] private TMP_Text _total;
        [SerializeField] private Image _identity;
        public void Render(string name, string opening, string penalty, string round, string total, Color identity)
        {
            _name.text = name;
            _opening.text = opening;
            _penalty.text = penalty;
            _round.text = round;
            _total.text = total;
            _identity.color = identity;
        }
    }
}
