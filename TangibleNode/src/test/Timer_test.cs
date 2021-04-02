using System;

namespace TangibleNode 
{
    class Timer_test 
    {
        public static void Run()
        {
            TTimer timer = new TTimer("testTimer");
            timer.Begin();
            if (timer.HasTimeSpanPassed) throw new Exception("TIME HAS PASSED [0]");
            timer.Begin();
            timer.Wait();
            Console.WriteLine("[0] MS between: " + timer.TimeBetween);
            Console.WriteLine("[0] MS passed: " + timer.TimePassed);
            Console.WriteLine("[0] Current timespan: " + timer.CurrentTimeSpan);
            Console.WriteLine("[0] Timer_test successfull");
            if (!timer.HasTimeSpanPassed) throw new Exception("TIME HAS NOT PASSED [0]");
            timer.Reset();
            timer.Wait();
            if (!timer.HasTimeSpanPassed) throw new Exception("TIME HAS NOT PASSED [1]");
            Console.WriteLine("[1] MS between: " + timer.TimeBetween);
            Console.WriteLine("[1] MS passed: " + timer.TimePassed);
            Console.WriteLine("[1] Current timespan: " + timer.CurrentTimeSpan);
            Console.WriteLine("[1] Timer_test successfull");
            timer.Reset();
            timer.End();
            if (timer.HasTimeSpanPassed) throw new Exception("TIME HAS PASSED [1]");
            // if (timer.TimeBetween != timer.CurrentTimeSpan) throw new Exception("TIME DID NOT PASS AS IT SHOULD");
            Console.WriteLine("[2] MS between: " + timer.TimeBetween);
            Console.WriteLine("[2] MS passed: " + timer.TimePassed);
            Console.WriteLine("[2] Current timespan: " + timer.CurrentTimeSpan);
            Console.WriteLine("[2] Timer_test successfull");
        }
    }
}