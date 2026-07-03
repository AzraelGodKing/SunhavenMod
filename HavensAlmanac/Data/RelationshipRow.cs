namespace HavensAlmanac.Data
{
    public sealed class RelationshipRow
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public float Points { get; set; }
        public bool Romanceable { get; set; }
        public bool IsDating { get; set; }
        public bool IsMarriedTo { get; set; }
        public bool IsPrimarySpouse { get; set; }
        public bool GiftedToday { get; set; }
    }
}
