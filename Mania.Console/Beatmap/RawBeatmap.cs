using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using static BeatmapEditor.Beatmap.OsuStatic;

namespace BeatmapEditor.Beatmap
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string category)
        {
            Category = category;
        }

        public string Category { get; set; }
    }
    public class RawBeatmap
    {
        public RawBeatmap()
        {

        }
        public static RawBeatmap Parse(string content)
        {
            var bm = new RawBeatmap();
            StringReader sr = new(content);
            sr.ReadLine();
            string Category = "";
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    Category = line.Substring(1, line.Length - 2);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;
                switch (Category)
                {
                    case "Events":
                        {
                            // Storyboard wont support.

                            // resolve break periods.
                            if (line.StartsWith("2")) // break time.
                            {
                                var args = line.Split(",");
                                bm.BreakPeriods.Add((double.Parse(args[1]), double.Parse(args[2])));
                            }
                            break;
                        }
                    case "TimingPoints":
                        {
                            TimingPoint tp = new();
                            var parts = line.Split(',');
                            var refl = tp.GetType().GetProperties();
                            for (int i = 0; i < parts.Length; i++)
                            {
                                refl[i].SetValue(tp, TypeDescriptor.GetConverter(refl[i].PropertyType).ConvertFromString(parts[i]));
                            }
                            bm.TimingPoints.Add(tp);
                            break;
                        }
                    case "HitObjects":
                        {
                            var parts = line.Split(',');
                            var refl = typeof(HitObject).GetProperties();
                            var type = parts[refl.ToList().IndexOf(refl.First(x => x.Name == nameof(HitObject.Type)))];
                            HitObject tp = ((HitObjectType)int.Parse(type) & (HitObjectType.Circle | HitObjectType.Slider | HitObjectType.Spinner | HitObjectType.Hold)) switch
                            {
                                HitObjectType.Circle => new HitObjectCircle(),
                                HitObjectType.Spinner => new HitObjectSpinner(),
                                HitObjectType.Slider => new HitObjectSlider(),
                                HitObjectType.Hold => new HitObjectHold(),
                                _ => throw new Exception()
                            };
                            refl = refl.Concat(tp.GetType().GetProperties()).DistinctBy(x=>x.Name).ToArray();
                            for (int i = 0; i < parts.Length && i < refl.Length; i++)
                            {
                                refl[i].SetValue(tp, TypeDescriptor.GetConverter(refl[i].PropertyType).ConvertFromString(parts[i]));
                            }
                            bm.HitObjects.Add(tp);
                            break;
                        }
                    default:
                        {
                            var key = line.Split(':')[0].Trim();
                            var value = string.Join(':', line.Split(':').Skip(1)).Trim();
                            var prop = bm.GetType().GetProperties().FirstOrDefault(x => x.Name == key);
                            if (prop?.GetCustomAttribute<CategoryAttribute>()?.Category == Category)
                                prop?.SetValue(bm, TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(value));
                            else
                                if (prop == null)
                                bm.Others.Add((Category, key, value));
                            break;
                        }
                }
            }
            return bm;
        }
        public List<(string, string, string)> Others { get; set; } = new();
        [Category("General")]
        public string AudioFilename { get; set; } = "";
        [Category("General")]
        public int PreviewTime { get; set; } = 0;
        [Category("General")]
        public string SampleSet { get; set; } = "";
        [Category("General")]
        public double StackLeniency { get; set; } = 0.7;
        [Category("General")]
        public int Countdown { get; set; } = 0;
        [Category("General")]
        public OsuGameMode Mode { get; set; } = OsuGameMode.Std;
        [Category("General")]
        public int WidescreenStoryboard { get; set; } = 1;

        [Category("Metadata")]
        public string TitleUnicode { get; set; } = "";
        [Category("Metadata")]
        public string ArtistUnicode { get; set; } = "";
        [Category("Metadata")]
        public string Creator { get; set; } = "";
        [Category("Metadata")]
        public string Version { get; set; } = "";

        [Category("Difficulty")]
        public double HPDrainRate { get; set; } = 0;
        [Category("Difficulty")]
        public double CircleSize { get; set; } = 0;
        [Category("Difficulty")]
        public double OverallDifficulty { get; set; } = 0;
        [Category("Difficulty")]
        public double ApproachRate { get; set; } = 0;
        [Category("Difficulty")]
        public double SliderMultiplier { get; set; } = 0;
        [Category("Difficulty")]
        public double SliderTickRate { get; set; } = 0;
        public List<(double, double)> BreakPeriods { get; set; } = new();

        public class TimingPoint
        {
            public double Time { get; set; }
            public double BeatLength { get; set; } = 100;
            public int TimeSignature { get; set; } = 4;
            public int SampleSet { get; set; }
            public SampleBank SampleBank { get; set; }
            public int SampleVolume { get; set; }
            public int TimingChange { get; set; }
            public EffectFlags Effects { get; set; }
            public double BPM => 60000 / BeatLength;
            public double SpeedMultiplier => BeatLength < 0 ? 100.0 / -BeatLength : 1;
            // 每节拍毫秒数
            public double MsPB => BeatLength / 60;
        }
        public List<TimingPoint> TimingPoints { get; set; } = new();
        public List<HitObject> HitObjects { get; set; } = new();
        public class HitObject
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double StartTime { get; set; }
            public HitObjectType Type { get; set; }
            public string? SoundType { get; set; }
        }
        public class HitObjectCircle : HitObject
        {
            public string? CustomSampleBanks { get; set; }
        }
        public class HitObjectHold : HitObject
        {
            public string? combined_param { get; set; }
            public double EndTime
            {
                get
                {
                    return double.Parse(combined_param.Split(":")[0]);
                }
            }
        }
        public class HitObjectSlider : HitObject
        {
            public string? PathRecord { get; set; }
            public int RepeatCount { get; set; }
            public double Length { get; set; }
            public string? CustomSampleBanks { get; set; }
        }
        public class HitObjectSpinner : HitObject
        {
            public double EndTime { get; set; }
            public string? CustomSampleBanks { get; set; }
        }
    }
}
