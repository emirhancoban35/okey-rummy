using System;
using Okey.Configuration;
using Okey.Presentation.Tables;
using UnityEngine;
namespace Okey.Composition
{
    [Serializable]
    public sealed class ScoreboardBinding
    {
        [SerializeField] private TableRuleProfile _profile;
        [SerializeField] private ScoreboardView _prefab;
        public TableRuleProfile Profile => _profile;
        public ScoreboardView Prefab => _prefab;
    }
}
