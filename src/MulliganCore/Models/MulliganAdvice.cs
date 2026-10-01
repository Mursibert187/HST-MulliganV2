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
        public double DrawnWinrate { get; }
        public double Confidence { get; }
        public DecisionGrade Grade { get; }
        public OpponentClass ScopedOpponent { get; }
        public bool ScopedByDeck { get; }
        public bool ScopedByCoinState { get; }

        public MulliganAdvice(
            MulliganCard card,
            KeepRateSample sample,
            double keepRate,
            double lift,
            double confidence,
            DecisionGrade grade,
            OpponentClass scopedOpponent,
            bool scopedByDeck,
            double drawnWinrate = double.NaN,
            bool scopedByCoinState = false)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Sample = sample;
            KeepRate = keepRate;
            Lift = lift;
            DrawnWinrate = drawnWinrate;
            Confidence = confidence;
            Grade = grade;
            ScopedOpponent = scopedOpponent;
            ScopedByDeck = scopedByDeck;
            ScopedByCoinState = scopedByCoinState;
        }

        public bool HasData => Sample.HasData;

        /// <summary>
        /// Delta between keeping and drawing the card later. This is the
        /// real "impact" number — more accurate than keptWR - baseWR because
        /// it reflects what the alternative is in this specific deck.
        /// Falls back to <see cref="Lift"/> when drawn winrate is missing.
        /// </summary>
        public double KeepVsDrawDelta =>
            !double.IsNaN(DrawnWinrate) && !double.IsNaN(Sample.KeptWinrate)
                ? Sample.KeptWinrate - DrawnWinrate
                : Lift;

        public override string ToString() =>
            $"{Card.Name}: {Grade} ({KeepRate:P0} impact {KeepVsDrawDelta:+0.00;-0.00;0.00} c={Confidence:F2})";
    }
}
