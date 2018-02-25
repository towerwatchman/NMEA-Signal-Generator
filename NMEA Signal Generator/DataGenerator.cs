using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NMEA_Signal_Generator
{
    class DataGenerator
    {
        //  public void GenerateData(string _type, int timer, double start, double end, double add, string header, bool reverse, bool rts, bool repeat, int pauseTime)
        public void CreateData(string type, int timer, double sTime, double eTime, double step, string header, bool reverse, bool rts, bool repeat, int pTime)
        {
            double loopStart = sTime > eTime ? sTime : eTime;
            double loopEnd = sTime > eTime ? eTime: sTime;

            if(type == "HDT")
            {

            }
            else if(type == "VBW")
            {

            }
        }
    }
}
