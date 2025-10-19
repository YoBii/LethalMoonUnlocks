using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalMoonUnlocks.Util {
    [Serializable]
    public class Notification {
        public Notification() { }
        [SerializeField]
        public string Header { get; init; } = "";
        [SerializeField]
        public string Text { get; init; } = "";
        [SerializeField]
        public bool IsWarning { get; init; } = false;
        [SerializeField]
        public bool UseSave { get; init; } = false;
        [SerializeField]
        public string Key { get; init; } = "LMU_";
        [SerializeField]
        public string ExceptWhenKey { get; init; } = "";
    }
}
