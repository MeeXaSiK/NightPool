using System;
using System.Threading.Tasks;

namespace NTC.Global.System.Tasks
{
    public static class TaskSugar
    {
        public static Task Delay(float time)
        {
            return Task.Delay(TimeSpan.FromSeconds(time));
        }
        
        public static Task Seconds(this float time)
        {
            return Task.Delay(TimeSpan.FromSeconds(time));
        }
    }
}