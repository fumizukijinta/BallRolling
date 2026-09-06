using System;
using UnityEngine;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ5のアイテム構築パラメータ（Step5_ItemSetupWindow で編集する）。</summary>
    [Serializable]
    public class Step5_PlayParameters
    {
        public float ItemSize = 0.3f;
        public float ItemLocalY = 0.35f;
        public float ItemRotationSpeedDegrees = 90f;
        public Color ItemColor = new Color(1f, 0.8f, 0.1f);
        public int ItemIndicatorFontSize = 36;
        public Color ItemIndicatorColor = new Color(1f, 0.8f, 0.1f);
        public string ItemIndicatorText = "ITEM GET!";
    }
}
