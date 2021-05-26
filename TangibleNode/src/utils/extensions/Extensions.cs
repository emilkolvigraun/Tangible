
namespace TangibleNode 
{
    public static class Extensions
    {
        ///<summary>Returns true if both types are the same</summary>
        public static bool REquals(this Call._Type t0, Call._Type t1)
        {
            if (t0 == t1) return true;
            if (t0.Equals(t1)) return true;
            return false;
        }
    }
}