using System;

namespace HstMulligan.Core.Models
{
    public enum DecisionGrade
    {
        Unknown = 0,
        StrongToss,
        Toss,
        Neutral,
        Keep,
        StrongKeep,
    }

    public sealed class MulliganAdvice
    {
        public MulliganCard Card { get; }
        public KeepRateSample Sample { get; }
        public double KeepRate { get; }
        public double Lift { get; }
        public double Confidence { get; }
        public DecisionGrade Grade { get; }
        public OpponentClass ScopedOpponent { get; }
        public bool ScopedByDeck { get; }

        public MulliganAdvice(
            MulliganCard card,
            KeepRateSample sample,
            double keepRate,
            double lift,
            double confidence,
            DecisionGrade grade,
            OpponentClass scopedOpponent,
            bool scopedByDeck)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Sample = sample;
            KeepRate = keepRate;
            Lift = lift;
            Confidence = confidence;
            Grade = grade;
            ScopedOpponent = scopedOpponent;
            ScopedByDeck = scopedByDeck;
        }

        public bool HasData => Sample.HasData;

        public override string ToString() =>
            $"{Card.Name}: {Grade} ({KeepRate:P0} lift {Lift:+0.00;-0.00;0.00} c={Confidence:F2})";
    }
}
