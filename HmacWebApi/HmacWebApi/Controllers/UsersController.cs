using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
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
        public IHttpActionResult PostUser(string username)
        {
            string responseContent = "";
            string data = Request.Content.ReadAsStringAsync().Result;
            string mediaType = Request.Content?.Headers?.ContentType?.MediaType?.ToLower() ?? "";
            var respMsg = Request.CreateResponse(System.Net.HttpStatusCode.OK);

            // ContentType : application/json
            if (mediaType.Length > 0 && mediaType.Equals("application/json"))
            {
                try
                {
                    JObject obj = new JObject();
                    obj["Username"] = username;
                    obj["Data"] = JObject.Parse(data);
                    responseContent = JsonConvert.SerializeObject(obj);
                }
                catch (Exception e)
                {
                    respMsg = Request.CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, e);
                    responseContent = data;
                }
            }
            // ContentType : application/x-www-form-urlencoded
            else if (mediaType.Length > 0 && mediaType.Equals("application/x-www-form-urlencoded"))
            {
                NameValueCollection valuePairs = HttpUtility.ParseQueryString(data);
                Dictionary<string, string> properties = valuePairs.AllKeys.ToDictionary(Key => Key, Key => valuePairs[Key]);

                responseContent = JsonConvert.SerializeObject(properties);
            }
            // ContentType : text/plain
            else if (mediaType.Length > 0 && mediaType.Equals("text/plain"))
            {
                responseContent = username + " says...\n\n" + data;
            }

            respMsg.Content = new StringContent(responseContent);
            respMsg.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return ResponseMessage(respMsg);
        }
    }
}
