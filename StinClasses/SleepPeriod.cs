using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses
{
    public class SleepPeriod
    {
        TimeSpan _startSleep;
        TimeSpan _endSleep;
        public SleepPeriod(TimeSpan startSleep, TimeSpan endSleep)
        {
            _startSleep = startSleep;
            _endSleep = endSleep;
        }
        public bool IsSleeping()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            if (_endSleep > _startSleep)
                return (currentTime >= _startSleep) && (currentTime < _endSleep);
            else
                return !((currentTime >= _endSleep) && (currentTime < _startSleep));
        }
    }
}
