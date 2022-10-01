using Microsoft.AspNetCore.Mvc.Filters;
using System.Xml.Linq;

namespace api.Filters
{
    public class AllowOriginAttribute: ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers["Access-Control-Allow-Origin"] =
                context.HttpContext.Request.Headers["Origin"];

            base.OnResultExecuting(context);
        }
    }
}
