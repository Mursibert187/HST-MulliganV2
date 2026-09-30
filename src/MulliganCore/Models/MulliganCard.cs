using System;

namespace HstMulligan.Core.Models
{
    public sealed class MulliganCard : IEquatable<MulliganCard>
    {
        public int DbfId { get; }
        public string CardId { get; }
        public string Name { get; }
        public int Cost { get; }
        public int SlotIndex { get; }

        public MulliganCard(int dbfId, string cardId, string name, int cost, int slotIndex)
        {
            DbfId = dbfId;
            CardId = cardId ?? string.Empty;
            Name = name ?? string.Empty;
            Cost = cost;
            SlotIndex = slotIndex;
        }

        public bool Equals(MulliganCard other) =>
            other != null && DbfId == other.DbfId && SlotIndex == other.SlotIndex;

        public override bool Equals(object obj) => Equals(obj as MulliganCard);
        public override int GetHashCode() => unchecked(DbfId * 397 ^ SlotIndex);
        public override string ToString() => $"{Name} (#{DbfId}) @ slot {SlotIndex}";
    }
}
