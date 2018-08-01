using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Hosting;
using System.Web.Http;

namespace HmacWebApi.Controllers
{
    public class DefaultController : ApiController
    {
        // Set the order to an abitrarily large number to ensure it is evaluated last.
        [HttpGet]
        public IHttpActionResult GetOutOfBounds(string pathInfo = "")
        {
            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK);
            var path = HostingEnvironment.MapPath("~/Default.html");
            response.Content = new StringContent("{ controller:  \"Default Controller\", pathInfo: \"" + pathInfo + "\" }\r\n\r\n" + File.ReadAllText(path));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return ResponseMessage(response);

        }
    }
}
