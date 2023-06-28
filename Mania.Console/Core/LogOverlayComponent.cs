using Osu.Console.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mania.Console.Core
{
    public class LogOverlayComponent : IGameController
    {
        private List<(double, string)> records=new();
        private int cursor = 0;
        public void PushMsg(string msg)
        {
            lock (records)
            {
                while (records.Count > 20)
                {
                    records.RemoveAt(0);
                }
                records.Add((255.0, msg));
            }
        }
        void IGameController.PushFrame(GameBuffer buffer) 
        {
            var slt = records.Where(x=>x.Item1 > 0);
            var rec_cnt = slt.Count();
            int i = 0;
            foreach (var item in (slt.Reverse()))
            {
                buffer.DrawString(item.Item2,((byte)item.Item1, (byte)item.Item1, (byte)item.Item1),1,buffer.Height - i - 1);
                i++;
            }
            List<(double, string)> removable=new();
            for (int j = 0; j < records.Count(); j++)
            {
                var rec = records[j];
                rec.Item1 -= 0.06;
                records[j] = rec;
            }

        }
    }
}
