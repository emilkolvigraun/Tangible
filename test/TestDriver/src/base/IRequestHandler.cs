using System.Collections.Generic;

namespace TangibleDriver 
{
    public interface IRequestHandler 
    {
        ValueResponse OnRequest(PointRequest request);
    }
}