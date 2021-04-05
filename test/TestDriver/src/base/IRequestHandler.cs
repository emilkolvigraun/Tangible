using System.Collections.Generic;

namespace TangibleDriver 
{
    public interface IRequestHandler 
    {
        List<ValueResponse> OnRequest(PointRequest request);
    }
}