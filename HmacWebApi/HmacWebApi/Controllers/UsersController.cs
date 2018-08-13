using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace HmacWebApi.Controllers
{
    public class UsersController : ApiController
    {
        /// <summary>
        /// Get user details from Active Directory.
        /// </summary>
        /// <param name="username">The username of the account.</param>
        /// <returns>An EndpointResult object which may contain info, warning, and error messages.</returns>
        [HttpGet, Route("api/users/{username}")]
        public IHttpActionResult GetUser(string username)
        {
            var responseMessage = Request.CreateResponse(System.Net.HttpStatusCode.OK);
            responseMessage.Content = new StringContent('"' + username + '"');
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return ResponseMessage(responseMessage);
        }

        /// <summary>
        /// Set user details in Active Directory.
        /// </summary>
        /// <param name="username">The username of the account.</param>
        /// <returns>An EndpointResult object which may contain info, warning, and error messages.</returns>
        [HttpPost, Route("api/users/{username}")]
        public IHttpActionResult PostUser(string username, [FromBody]string data)
        {
            var responseMessage = Request.CreateResponse(System.Net.HttpStatusCode.OK);
            string respContent = "{\n" +
                                $"    \"Name\":  \"{ username }\",\n" +
                                $"    \"Data\":  \"{ data }\"\n" +
                                 "}";

            responseMessage.Content = new StringContent('"' + username + '"');
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return ResponseMessage(responseMessage);
        }
    }
}
