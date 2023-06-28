using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatmapEditor.Beatmap
{
    public static class OsuStatic
    {
        #region "Defines"
        public const float OBJECT_RADIUS = 64;
        public const float BASE_SCORING_DISTANCE = 100;
        public const double PREEMPT_MIN = 450;
        public const float DEFAULT_DIFFICULTY = 5;
        public const double FADE_OUT_DURATION = 200;
        public const double FADE_OUT_SCALE = 1.5;
        public static double DifficultyRange(double difficulty, double min, double mid, double max)
        {
            if (difficulty > 5)
                return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5)
                return mid - (mid - min) * (5 - difficulty) / 5;

            return mid;
        }
        public static double DifficultyRange(double difficulty, (double od0, double od5, double od10) range)
            => DifficultyRange(difficulty, range.od0, range.od5, range.od10);
        public static double DifficultyPreempt(double ar)
        {
            return DifficultyRange(ar, 1800, 1200, PREEMPT_MIN);
        }
        public static double DifficultyFadeIn(double preempt)
        {
            return 400 * System.Math.Min(1, preempt / PREEMPT_MIN);
        }
        public static double DifficultyScale(double cs)
        {
            return (1.0f - 0.7f * (cs - 5) / 5) / 2;
        }
        public static readonly HitRange[] BaseHitRanges = {
            new HitRange(HitResult.Perfect, 22.4D, 19.4D, 13.9D),
            new HitRange(HitResult.Great, 64, 49, 34),
            new HitRange(HitResult.Good, 97, 82, 67),
            new HitRange(HitResult.Ok, 127, 112, 97),
            new HitRange(HitResult.Meh, 151, 136, 121),
            new HitRange(HitResult.Miss, 188, 173, 158),
        };
        public static Dictionary<HitResult, double> GetHitRanges(double od)
            => new Dictionary<HitResult, double>(
                BaseHitRanges.Select(x =>
                new KeyValuePair<HitResult, double>(x.Result, DifficultyRange(od, (x.Min, x.Average, x.Max)))
                )
            );
        #endregion
    }
    public class HitRange
    {
        public HitRange(HitResult result, double min, double average, double max)
        {
            Result = result;
            Min = min;
            Average = average;
            Max = max;
        }
        public HitResult Result { get; set; }
        public double Min { get; set; }
        public double Average { get; set; }
        public double Max { get; set; }
    }
    public enum HitResult
    {
        None,
        // 0
        Miss,
        // 50
        Meh,
        // 100
        Ok,
        // 200
        Good,
        // 300
        Great,
        // 320
        Perfect,
        // 0
        SmallTickMiss,
        SmallTickHit,
        LargeTickMiss,
        LargeTickHit,
        SmallBonus,
        LargeBonus,
        IgnoreMiss,
        IgnoreHit,
    }
    [Flags]
    public enum HitSoundType
    {
        None = 0,
        Normal = 1,
        Whistle = 2,
        Finish = 4,
        Clap = 8
    }
    [Flags]
    public enum HitObjectType
    {
        Circle = 1,
        Slider = 1 << 1,
        NewCombo = 1 << 2,
        Spinner = 1 << 3,
        ComboOffset = (1 << 4) | (1 << 5) | (1 << 6),
        Hold = 1 << 7
    }
    public enum PathType
    {
        Catmull = 'C',
        Bezier = 'B',
        Linear = 'L',
        PerfectCurve = 'P',
    }
    public enum SampleBank
    {
        None = 0,
        Normal = 1,
        Soft = 2,
        Drum = 3
    }
    [Flags]
    public enum EffectFlags
    {
        None = 0,
        Kiai = 1,
        OmitFirstBarLine = 8
    }
    public enum OsuGameMode
    {
        Std = 1,
        Taiko = 2,
        Catch = 3,
        Mania = 4,
    }
}
