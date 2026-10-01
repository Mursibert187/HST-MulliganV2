using System;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Live
{
    public sealed class KeepRateCalculatorOptions
    {
        public double StrongKeepAt { get; set; } = 0.75;
        public double KeepAt { get; set; } = 0.55;
        public double NeutralHigh { get; set; } = 0.55;
        public double NeutralLow { get; set; } = 0.45;
        public double StrongTossAt { get; set; } = 0.25;
        public double LiftPromoteStrong { get; set; } = 0.03;
        public double LiftPromoteWeak { get; set; } = 0.015;
    }

    public sealed class KeepRateCalculator
    {
        private readonly ConfidenceScorer _scorer;
        private readonly KeepRateCalculatorOptions _opts;

        public KeepRateCalculator(ConfidenceScorer scorer = null, KeepRateCalculatorOptions options = null)
        {
            _scorer = scorer ?? new ConfidenceScorer();
            _opts = options ?? new KeepRateCalculatorOptions();
        }

        public MulliganAdvice Advise(MulliganCard card, MulliganDataset dataset, MulliganContext ctx)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (dataset == null) dataset = MulliganDataset.Empty;
            if (ctx == null) ctx = MulliganContext.Unknown;

            if (!dataset.TryGet(card.DbfId, out var stats))
            {
                return new MulliganAdvice(
                    card, KeepRateSample.Empty, double.NaN, 0.0, 0.0,
                    DecisionGrade.Unknown, ctx.Opponent, scopedByDeck: false);
            }

            var effectiveOpponent = ctx.EffectiveOpponent;
            var sample = stats.Resolve(effectiveOpponent, ctx.DeckCode, ctx.ArchetypeId);
            var scopedByDeck = !string.IsNullOrEmpty(ctx.DeckCode)
                && stats.ByDeck.TryGetValue(ctx.DeckCode, out var deckSample)
                && deckSample.HasData;

            if (!sample.HasData)
            {
                return new MulliganAdvice(
                    card, sample, double.NaN, 0.0, 0.0,
                    DecisionGrade.Unknown, ctx.Opponent, scopedByDeck);
            }

            var keepRate = sample.KeepRate;
            var lift = double.IsNaN(sample.KeptWinrate) ? 0.0 : sample.KeptWinrate - dataset.BaseWinrate;
            var confidence = _scorer.Score(sample.Total);
            var grade = BucketGrade(keepRate, lift);
            return new MulliganAdvice(card, sample, keepRate, lift, confidence, grade, ctx.Opponent, scopedByDeck);
        }

        public DecisionGrade BucketGrade(double keepRate, double lift)
        {
            if (double.IsNaN(keepRate)) return DecisionGrade.Unknown;
            if (keepRate >= _opts.StrongKeepAt) return DecisionGrade.StrongKeep;
            if (keepRate >= 0.65 && lift >= _opts.LiftPromoteStrong) return DecisionGrade.StrongKeep;
            if (keepRate >= _opts.KeepAt) return DecisionGrade.Keep;
            if (lift >= _opts.LiftPromoteWeak && keepRate >= _opts.NeutralLow) return DecisionGrade.Keep;
            if (keepRate < _opts.StrongTossAt) return DecisionGrade.StrongToss;
            if (keepRate < 0.35 && lift <= -_opts.LiftPromoteStrong) return DecisionGrade.StrongToss;
            if (keepRate < _opts.NeutralLow) return DecisionGrade.Toss;
            if (lift <= -_opts.LiftPromoteWeak) return DecisionGrade.Toss;
            return DecisionGrade.Neutral;
        }
    }
}
