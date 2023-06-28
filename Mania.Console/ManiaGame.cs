using BeatmapEditor.Audio;
using BeatmapEditor.Beatmap;
using Osu.Console.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Color = System.ValueTuple<byte, byte, byte>;

namespace Mania.Console
{
    public class ManiaGame : IGameController
    {
        public ManiaGame()
        {
            bam.OpenDevice(bam.GetDefaultDevice());
            KeyBinds.AddRange(Enumerable.Range(KeyBinds.Count, 20 - KeyBinds.Count).Select(x => Enumerable.Range(0, x + 1).Select(_ => ConsoleKey.NoName).ToList()));
            if (!File.Exists("config.json"))
            {
                cfg = new();
                cfg.KeyBinds = KeyBinds;
                Save();
            }
            cfg = JsonSerializer.Deserialize<GameCfg>(File.ReadAllText("config.json"));
            KeyBinds = cfg.KeyBinds;
        }
        private class GameCfg
        {
            public List<List<ConsoleKey>> KeyBinds = new() { new() { ConsoleKey.Spacebar }, new() { ConsoleKey.D,ConsoleKey.F},
        new(){ConsoleKey.D,ConsoleKey.Spacebar,ConsoleKey.F},new(){ ConsoleKey.D, ConsoleKey.F, ConsoleKey.J, ConsoleKey.K },
        new(){ ConsoleKey.D, ConsoleKey.F,ConsoleKey.Spacebar, ConsoleKey.J, ConsoleKey.K },new(){ ConsoleKey.S,ConsoleKey.D, ConsoleKey.F, ConsoleKey.J, ConsoleKey.K ,ConsoleKey.L},
        new(){ ConsoleKey.S,ConsoleKey.D, ConsoleKey.F,ConsoleKey.Spacebar, ConsoleKey.J, ConsoleKey.K ,ConsoleKey.L}};
            public string? songs_path;
        }
        private GameCfg? cfg;
        private void Save()
        {
            if (cfg != null)
            {
                var str = JsonSerializer.Serialize<GameCfg>(cfg, new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true, Converters = { new JsonStringEnumConverter() } });
                File.WriteAllText("config.json", str);
            }
        }
        private int screen = 0;// 0=welcome 1=song select 2=game play 3=game play pause 4=key bind
        private BassAudioManager bam = new();
        private int keybind_key_count = -1;
        private int keybind_index = 0;
        private List<List<ConsoleKey>> KeyBinds = new() { new() { ConsoleKey.Spacebar }, new() { ConsoleKey.D,ConsoleKey.F},
        new(){ConsoleKey.D,ConsoleKey.Spacebar,ConsoleKey.F},new(){ ConsoleKey.D, ConsoleKey.F, ConsoleKey.J, ConsoleKey.K },
        new(){ ConsoleKey.D, ConsoleKey.F,ConsoleKey.Spacebar, ConsoleKey.J, ConsoleKey.K },new(){ ConsoleKey.S,ConsoleKey.D, ConsoleKey.F, ConsoleKey.J, ConsoleKey.K ,ConsoleKey.L},
        new(){ ConsoleKey.S,ConsoleKey.D, ConsoleKey.F,ConsoleKey.Spacebar, ConsoleKey.J, ConsoleKey.K ,ConsoleKey.L}};
        private bool song_cache_not_ready = true;
        private bool song_cache_building = false;
        private List<(string path, string title, string[] diff)> Songs_str = new();
        private double song_position = 0;
        private double diff_position = 0;
        private string? current_song_path;
        private RawBeatmap? beatmap;
        private double bmp_duration;
        private List<ManiaObject>? objects;
        private class ManiaObject
        {
            public double StartTime;
            public double EndTime;
            public int Column;
            public bool HasHit = false;
            public bool SlideFinish = false;
        }
        private List<ManiaSpeedChange>? timelines;
        private class ManiaSpeedChange
        {
            public double mspb;
            public double StartTime;
            public double Speed;
        }
        private double hiterr_anim_clk = 0;
        private HitResult hiterr_ui = HitResult.Perfect;
        private double scrollspeed = 750; // 1s
        private Stopwatch sw_bmp = new();
        private int combo;
        private int score;
        void IGameController.PushFrame(GameBuffer buffer)
        {
            if (screen == 0)
            {
                buffer.DrawRect((' ', ((255, 255, 255), default)), (0, 0, buffer.Width - 1, buffer.Height - 1));
                var banner = "OsuManiaConsole (https://github.com/telecomadm1145)";
                buffer.DrawString(banner, (default, (255, 255, 255)), 1, buffer.Height - 2);
                buffer.DrawString("Press enter to song select.\nPress X to key bind.", (default, (255, 255, 255)), 1, 1);
            }
            if (screen == 1)
            {
                if (song_cache_not_ready)
                {
                    if (cfg != null && !Path.Exists(cfg.songs_path))
                    {
                        buffer.DrawString($"Please drag songs path in.\n(if nothing happened,please press enter)\n{songs_input_buf}", (default, (255, 255, 255)), 0, 0);
                        return;
                    }
                    buffer.DrawString("Loading...", (default, (255, 255, 255)), 0, 0);
                    return;
                }
                int extra = 0;
                string value = search_cache.ToString();
                var filtered = Songs_str.Where(x =>
                {
                    return x.title.Contains(value);
                }).ToList();
                for (int i = 0; i < filtered.Count; i++)
                {
                    (Color, Color) clrinf = (default, (255, 255, 255));
                    bool iscurrent = Math.Abs(i + song_position) < 0.5;
                    if (iscurrent && (-diff_position >= filtered[i].diff.Length))
                    {
                        song_position--;
                        diff_position = 0;
                    }
                    buffer.DrawString(filtered[i].title, !iscurrent ? clrinf : (clrinf.Item2, clrinf.Item1), 0, i + (int)song_position + buffer.Height / 2 + extra);
                    if (iscurrent)
                    {
                        string[] diff = filtered[i].diff;
                        for (int j = 0; j < diff.Length; j++)
                        {
                            var iscurrent2 = Math.Abs(j + diff_position) < 0.5;
                            buffer.DrawString("---", clrinf, 0, i + (int)song_position + buffer.Height / 2 + j + 1);
                            buffer.DrawString(diff[j], !iscurrent2 ? clrinf : (clrinf.Item2, clrinf.Item1), 3, i + (int)song_position + buffer.Height / 2 + j + 1);
                        }
                        extra += diff.Length;
                    }
                    if (i + song_position > buffer.Height)
                        break;
                }
            }
            if (screen == 2)
            {
                if (loading_status != 999)
                {
                    buffer.DrawString("Loading...", (default, (255, 255, 255)), 0, 0);
                    return;
                }
                long e_ms = sw_bmp.ElapsedMilliseconds;
                var visble_objects = objects.Where(x => (x.StartTime + scrollspeed / 2 >= e_ms && x.StartTime <= e_ms + scrollspeed) ||
                (x.EndTime == -1 ? false : x.StartTime <= e_ms + scrollspeed && x.EndTime >= e_ms)).ToList();
                int keys = (int)beatmap.CircleSize;
                double key_width = 10;// 4 宽度
                double centre = (double)buffer.Width / 2;
                double centre_start = centre - (keys * key_width) / 2;
                double judge_height = 4;
                var j = 0;
                for (double i = centre_start; i < keys * key_width + centre_start; i += key_width)
                {
                    var v1 = (int)((255 - 179) * Math.Max(HitLightning[j], 0));
                    var value = ((byte)(179 + v1), (byte)(179 + v1), (byte)(179 + v1));
                    buffer.FillRect((' ', (value, default)), ((int)i + 1, (int)(buffer.Height - judge_height + 1), (int)(i + key_width), buffer.Height));
                    foreach (var obj in visble_objects.Where(y => y.Column == j))
                    {
                        var ratio = 1 - (obj.StartTime - e_ms) / scrollspeed;
                        var starty = ratio * (buffer.Height - judge_height);
                        if (obj.EndTime != -1 && !obj.SlideFinish)
                        {
                            var endy = (1 - (obj.EndTime - e_ms) / scrollspeed) * (buffer.Height - judge_height);
                            buffer.FillRect((' ', ((0, 140, 179), default)), ((int)i + 1, (int)Math.Min(starty, buffer.Height - judge_height), (int)(i + key_width), (int)endy));
                            buffer.FillRect((' ', ((0, 160, 230), default)), ((int)i + 1, (int)Math.Min(starty, buffer.Height - judge_height), (int)(i + key_width), (int)Math.Min(starty , buffer.Height - judge_height)));
                        }
                        if (!obj.HasHit && obj.EndTime == -1)
                            buffer.FillRect((' ', ((0, 160, 230), default)), ((int)i + 1, (int)starty, (int)(i + key_width), (int)starty));
                    }
                    buffer.DrawLineVertical(('-', (value, (255, 255, 255))), (int)i, (int)(i + key_width), (int)(buffer.Height - judge_height + 1));
                    buffer.DrawRect((' ', ((255, 255, 255), default)), ((int)i, -1, (int)(i + key_width), buffer.Height));
                    hiterr_anim_clk -= 0.0005;
                    if (hiterr_anim_clk >= 0)
                    {
                        string v = hiterr_ui.ToString();
                        buffer.DrawString(v, ((byte)(hiterr_anim_clk * 255), (byte)(hiterr_anim_clk * 255), (byte)(hiterr_anim_clk * 255)), (buffer.Width - v.Length) / 2, buffer.Height / 2);
                    }
                    j++;
                }
                var v3 = ResCounter.Count;
                int k = 0;
                foreach (var kv in ResCounter)
                {
                    buffer.DrawString(kv.Key.ToString() + ":" + kv.Value.ToString(), (255, 255, 255), 0, (buffer.Height - v3) / 2 + k);
                    k++;
                }
                var mean = HitErrors.Count != 0 ? HitErrors.Sum() / HitErrors.Count : 0;
                buffer.DrawString(mean.ToString("F1") + "ms", (255, 255, 255), 0, buffer.Height - 1);
                buffer.DrawString(combo.ToString() + "x", (255, 255, 255), 0, buffer.Height - 2);
                string v2 = score.ToString();
                buffer.DrawString(v2, (255, 255, 255), buffer.Width - v2.Length - 1, 0);
            }
            if (screen == 3)
            {
                buffer.DrawString("Paused,press any key to resume in 3s.", (255, 255, 255), 0, 0);
            }
            if (screen == 4)
            {
                if (keybind_key_count == -1)
                    buffer.DrawString("Input the number of keys you want configure.\n(From 1 to 10 means 1-10,from F1 to F10 means 11-20,Esc to exit)", (default, (255, 255, 255)), 0, 0);
                else
                    buffer.DrawString($"Press the key for the {keybind_index + 1}th column.\n(Esc to quit,Now binds are {{{string.Join(' ', KeyBinds[keybind_key_count])}}}({keybind_key_count + 1} Key(s)))", (default, (255, 255, 255)), 0, 0);
            }
            if (screen == 5)
            {
                buffer.DrawString($"Score:{score}\nMaxCombo:{maxcombo}\n\n{string.Join('\n',ResCounter.Reverse().Select(x=>$"{x.Key}:{x.Value}"))}\n\nPress enter to song select.", (255, 255, 255), 0, 0);
            }
        }
        private int maxcombo;
        private IAudioManager.IAudioStream? track;
        private int loading_status = 0;
        private StringBuilder search_cache = new();
        void IGameController.Tick(System.TimeSpan fromRun)
        {
            if (screen == 1)
            {
                if (song_cache_not_ready)
                {
                    if (!song_cache_building && cfg != null && cfg.songs_path != null && Path.Exists(cfg.songs_path))
                    {
                        Task.Run(() =>
                        {
                            Songs_str.Clear();
                            Songs_str.AddRange(Directory.EnumerateDirectories(cfg.songs_path).Select(x => (x, Directory.CreateDirectory(x).Name, Directory.EnumerateFiles(x).Where(x => x.EndsWith(".osu", StringComparison.CurrentCultureIgnoreCase)).Select(x => Path.GetFileName(x)).ToArray())));
                            song_cache_building = false;
                            song_cache_not_ready = false;
                        });
                        song_cache_building = true;
                    }
                }
            }
            if (screen == 2)
            {
                if (loading_status == 0)
                {
                    if (track != null)
                        track.Dispose();
                    track = bam.Load(File.ReadAllBytes(Path.Combine(Directory.GetParent(current_song_path).FullName, beatmap.AudioFilename))); // play while loading(
                    track.Play();
                    track.Current = TimeSpan.FromMilliseconds(beatmap.PreviewTime);
                    track.Volume = 0.75;
                    loading_status = 1;
                    HitRanges = OsuStatic.GetHitRanges(beatmap.OverallDifficulty);
                }
                if (loading_status == 1)
                {
                    Task.Run(() =>
                    {
                        timelines = new();
                        var mspb = 5.0;
                        var lst_spd = -999.0;
                        var keys = beatmap.CircleSize;
                        foreach (var timeline in beatmap.TimingPoints)
                        {
                            if (timeline.TimingChange > 0)
                                mspb = timeline.MsPB;
                            if (lst_spd != timeline.SpeedMultiplier)
                            {
                                timelines.Add(new() { StartTime = timeline.Time, mspb = mspb, Speed = timeline.SpeedMultiplier });
                                lst_spd = timeline.SpeedMultiplier;
                            }
                        }
                        objects = new();
                        foreach (var obj in beatmap.HitObjects)
                        {
                            if (obj is RawBeatmap.HitObjectCircle circle)
                            {
                                objects.Add(new() { EndTime = -1, StartTime = circle.StartTime, Column = CalcColumn(obj.X, (int)keys) });
                                bmp_duration = Math.Max(bmp_duration, circle.StartTime);
                            }
                            if (obj is RawBeatmap.HitObjectHold hold)
                            {
                                objects.Add(new() { EndTime = hold.EndTime, StartTime = hold.StartTime, Column = CalcColumn(obj.X, (int)keys) });
                                bmp_duration = Math.Max(bmp_duration, hold.EndTime);
                            }
                        }
                        //foreach (var obj in objects)
                        //{
                        //    obj.StartTime = CalcRelativePos(obj.StartTime);
                        //    if (obj.EndTime != -1)
                        //    {
                        //        obj.EndTime = CalcRelativePos(obj.EndTime);
                        //    }
                        //}
                        //计算相对时间（
                        track.Dispose();
                        track = bam.Load(File.ReadAllBytes(Path.Combine(Directory.GetParent(current_song_path).FullName, beatmap.AudioFilename))); // play while loading(
                        Task.Delay(3000).ContinueWith(t => { track.Play(); sw_bmp.Start(); });
                        loading_status = 999;
                    });
                    loading_status = 2;
                }
                if (loading_status == 999)
                {
                    long now_clock = sw_bmp.ElapsedMilliseconds;
                    var automiss = objects.Where(x => x.StartTime - now_clock <= -HitRanges[HitResult.Meh] && !x.HasHit);
                    foreach (var item in automiss)
                    {
                        ApplyHit(item, HitResult.Miss, 0);
                    }
                    var keys = (int)beatmap.CircleSize;
                    for (int i = 0; i < keys; i++)
                    {
                        var pressed = KeyStatus[i];
                        if (pressed)
                        {
                            HitLightning[i] = 1;
                        }
                        else
                        {
                            HitLightning[i] -= 0.002;
                        }
                    }
                    if (now_clock >= bmp_duration + 1000)
                    {
                        screen = 5;
                    }
                }
            }
        }

        int CalcColumn(double xpos, int keys)
        {
            double begin = 512 / keys / 2;
            double mid = begin;
            for (int i = 0; i < keys; i++)
            {
                if (Math.Abs(mid - xpos) < begin)
                {
                    return i;
                }
                mid += begin * 2;
            }
            return 0;
        }
        void IGameController.Wheel(double delta, Osu.Console.Core.IGameController.WheelDirection dir)
        {
            song_position += delta;
        }
        private StringBuilder songs_input_buf = new();
        private Dictionary<HitResult, double>? HitRanges;
        private List<double> HitLightning = new(Enumerable.Range(0, 20).Select(x => 0.0));
        private List<bool> KeyStatus = new(Enumerable.Range(0, 20).Select(x => false));
        void IGameController.Key(KeyEvent cki)
        {
            if (screen == 0)
            {
                if (cki.Pressed)
                {
                    if (cki.Key == ConsoleKey.Enter)
                    {
                        screen = 1;
                        return;
                    }
                    if (cki.Key == ConsoleKey.X)
                    {
                        screen = 4;
                        return;
                    }
                }
            }
            if (screen == 1)
            {
                if (song_cache_not_ready)
                {
                    if (cfg != null && !Path.Exists(cfg.songs_path))
                    {
                        if (cki.Pressed)
                        {
                            if (cki.Key == ConsoleKey.Enter)
                            {
                                cfg.songs_path = songs_input_buf.ToString();
                                songs_input_buf.Clear();
                                return;
                            }
                            if (cki.Key == ConsoleKey.Backspace)
                            {
                                if (songs_input_buf.Length >= 1)
                                    songs_input_buf.Remove(songs_input_buf.Length - 1, 1);
                                return;
                            }
                            if (cki.UnicodeChar != '\0')
                                songs_input_buf.Append(cki.UnicodeChar);
                        }
                    }
                    return;
                }
                if (cki.Pressed)
                {
                    if (cki.Key == ConsoleKey.UpArrow)
                    {
                        diff_position++;
                        return;
                    }
                    if (cki.Key == ConsoleKey.DownArrow)
                    {
                        diff_position--;
                        return;
                    }
                    if (cki.Key == ConsoleKey.LeftArrow)
                    {
                        diff_position = 0;
                        song_position++;
                        return;
                    }
                    if (cki.Key == ConsoleKey.RightArrow)
                    {
                        diff_position = 0;
                        song_position--;
                        return;
                    }
                    if (cki.Key == ConsoleKey.Enter)
                    {
                        int v = (int)Math.Round(-song_position, 0);
                        string value = search_cache.ToString();
                        var filtered = Songs_str.Where(x =>
                        {
                            return x.title.Contains(value);
                        }).ToList();
                        if (v >= 0 && v <= filtered.Count)
                        {
                            var song = filtered[v];
                            int v2 = (int)Math.Round(-diff_position, 0);
                            if (v2 >= 0 && v2 <= song.diff.Length)
                            {
                                current_song_path = Path.Combine(song.path, song.diff[v2]);
                                beatmap = RawBeatmap.Parse(File.ReadAllText(Path.Combine(song.path, song.diff[v2])));
                                screen = 2;
                            }
                        }
                        return;
                    }
                    if (cki.Key == ConsoleKey.Backspace)
                    {
                        if (search_cache.Length >= 1)
                            search_cache.Remove(search_cache.Length - 1, 1);
                        return;
                    }
                    if (cki.UnicodeChar != '\0')
                        search_cache.Append(cki.UnicodeChar);
                }
                return;
            }
            if (screen == 2)
            {
                if (cki.Pressed)
                {
                    if (cki.Key == ConsoleKey.Escape)
                    {
                        sw_bmp.Stop();
                        track?.Pause();
                        screen = 3;
                    }
                    if (cki.Key == ConsoleKey.F3)
                    {
                        const double c = 12000;
                        scrollspeed = c / (c / scrollspeed - 1);
                    }
                    if (cki.Key == ConsoleKey.F4)
                    {
                        const double c = 12000;
                        scrollspeed = c / (c / scrollspeed + 1);
                    }
                }
                // judges
                var keys = (int)beatmap.CircleSize;

                for (int i = 0; i < KeyBinds[keys - 1].Count; i++)
                {
                    if (cki.Key == KeyBinds[keys - 1][i])
                    {
                        KeyStatus[i] = cki.Pressed;
                        long now_clock = sw_bmp.ElapsedMilliseconds;
                        //var slideobj = objects.OrderBy(x => x.StartTime).FirstOrDefault(x => x.StartTime <= now_clock && x.Column == i && x.EndTime >= now_clock && x.HasHit && !x.SlideFinish && x.EndTime != -1);
                        //if (slideobj != null)
                        //{
                        //    if (slideobj.StartTime <= now_clock && !((slideobj.EndTime - slideobj.StartTime) < 100 || (slideobj.EndTime - now_clock) / (slideobj.StartTime - slideobj.EndTime) < 0.2))
                        //    {
                        //        slideobj.SlideFinish = true;
                        //        hiterr_anim_clk = 1;
                        //        hiterr_ui = HitResult.Miss;
                        //    }
                        //    else
                        //    {
                        //        slideobj.SlideFinish = true;
                        //        hiterr_anim_clk = 1;
                        //        hiterr_ui = HitResult.Perfect;
                        //    }
                        //    return;
                        //}
                        var obj = objects.OrderBy(x => x.StartTime).FirstOrDefault(x =>
                        {
                            return Math.Abs(x.StartTime - now_clock) < 400 && !x.HasHit && x.Column == i;
                        }); // 抓取 400 ms 以内第一个未击打的物件
                        if (obj == null)
                            return;
                        double err = obj.StartTime - now_clock;
                        Debug.Write(i);
                        Debug.Write(' ');
                        Debug.WriteLine(err);
                        if (cki.Pressed)
                        {
                            if (-err >= HitRanges[HitResult.Meh]) // 超延后击打,强行miss
                            {
                                ApplyHit(obj, HitResult.Miss, err);
                                return;
                            }
                            foreach (var res in ResCounter.Keys.Reverse())
                            {
                                if (Math.Abs(err) <= HitRanges[res])
                                {
                                    ApplyHit(obj, res, err);
                                    return;
                                }
                            }
                        }
                    }
                }
                return;
            }
            if (screen == 3)
            {
                if (cki.Pressed)
                {
                    if (cki.Key == ConsoleKey.Escape)
                    {
                        ReturnToSongSelect();
                        return;
                    }
                    screen = 2;
                    Task.Delay(3000).ContinueWith(t =>
                    {
                        if (track != null)
                        {
                            sw_bmp.Start();
                            track.Play();
                        }
                    });
                }
                return;
            }
            if (screen == 4)
            {
                if (cki.Pressed)
                {
                    if (cki.Key == ConsoleKey.Escape)
                    {
                        keybind_key_count = -1;
                        keybind_index = 0;
                        screen = 0;
                        return;
                    }
                    if (keybind_key_count == -1)
                    {
                        var v1 = cki.Key - ConsoleKey.D1;
                        if (v1 >= 0 && v1 <= 8)
                        {
                            keybind_key_count = v1;
                            return;
                        }
                        if (v1 == -1)
                        {
                            keybind_key_count = 9;
                            return;
                        }
                        var v2 = cki.Key - ConsoleKey.F1;
                        if (v2 >= 0 && v2 <= 9)
                        {
                            keybind_key_count = v2 + 10;
                            return;
                        }
                        return;
                    }
                    if (keybind_index >= KeyBinds[keybind_key_count].Count)
                    {
                        KeyBinds[keybind_key_count].Add(cki.Key);
                    }
                    else
                    {
                        KeyBinds[keybind_key_count][keybind_index] = cki.Key;
                    }
                    if (keybind_index >= keybind_key_count)
                    {
                        keybind_key_count = -1;
                        keybind_index = 0;
                        screen = 0;
                        return;
                    }
                    keybind_index++;
                    return;
                }
                return;
            }
            if (screen == 5)
            {
                if (cki.Pressed && cki.Key == ConsoleKey.Enter)
                {
                    ReturnToSongSelect();
                    return;
                }
            }
        }

        private void ReturnToSongSelect()
        {
            score = 0;
            combo = 0;
            foreach (var key in ResCounter.Keys)
            {
                ResCounter[key] = 0;
            }
            HitErrors.Clear();
            hiterr_anim_clk = 0;
            sw_bmp.Reset();
            track.Dispose();
            loading_status = 0;
            track = null;
            maxcombo = 0;
            screen = 1;
        }

        private Dictionary<HitResult, int> ResCounter = new() { { HitResult.Miss, 0 }, { HitResult.Meh, 0 }, { HitResult.Ok, 0 }, { HitResult.Good, 0 }, { HitResult.Great, 0 }, { HitResult.Perfect, 0 } };
        private int GetBaseScore(HitResult res)
        {
            switch (res)
            {
                case HitResult.Miss:
                    return 0;
                case HitResult.Meh:
                    return 50;
                case HitResult.Ok:
                    return 100;
                case HitResult.Good:
                    return 200;
                case HitResult.Great:
                    return 300;
                case HitResult.Perfect:
                    return 320;
                default:
                    return 20;
            }
        }
        private List<double> HitErrors = new();
        private void ApplyHit(ManiaObject mo, HitResult res, double err)
        {
            mo.HasHit = true;
            hiterr_anim_clk = 1;
            hiterr_ui = res;
            ResCounter[res]++;
            HitErrors.Add(err);
            if (res != HitResult.Miss)
            {
                combo++;
            }
            else
            {
                combo = 0;
            }
            maxcombo = Math.Max(maxcombo, combo);
            score += GetBaseScore(res) * (int)Math.Max(1,Math.Pow(combo,0.1));
        }
        void IGameController.Init(Osu.Console.Game game)
        {

        }
    }
}
