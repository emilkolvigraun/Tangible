
namespace TangibleNode 
{
    class Test 
    {
        public int DieAsFollower_MS {get; set;}
        public int DieAsLeader_MS {get; set;}
        public bool RunHIE {get; set;}
        public int CommitFrequency {get; set;} = 2000;
        public string TestReceiverHost {get; set;} = string.Empty;
        public int TestReceiverPort {get; set;} = 4000;
    }
}