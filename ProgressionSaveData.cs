namespace LethalMoonUnlocks {
    [System.Serializable]
    internal class ProgressionSaveData {
        internal int PaintingsSold { get; set; }

        internal bool HasData() {
            return PaintingsSold > 0;
        }
    }
}
