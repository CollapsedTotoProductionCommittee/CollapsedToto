using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using DotNetOpenAuth.Messaging;
using Nancy;
using System.Web;

namespace CollapsedToto
{
    static public class Utils
    {
        public static Response AsNancyResponse(this OutgoingWebResponse openauthResponse) 
        {
            var response = new Response();

            var statusCode = HttpStatusCode.InternalServerError;
            string statusCodeName = null;
            if (openauthResponse.Status == System.Net.HttpStatusCode.Redirect)
            {
                statusCodeName = HttpStatusCode.TemporaryRedirect.ToString();
            }
            else
            {
                statusCodeName = openauthResponse.Status.ToString();
            }

            if (!Enum.TryParse<HttpStatusCode>(statusCodeName, out statusCode))
            {
                throw new Exception("Unknown Status Code: " + openauthResponse.Status.ToString());
            }
            response.StatusCode = statusCode;

            var headers = new Dictionary<string, string>();
            foreach(var key in openauthResponse.Headers.AllKeys)
            {
                headers.Add(key, openauthResponse.Headers[key]);
            }
            response.Headers = headers;

            var body = Encoding.UTF8.GetBytes(openauthResponse.Body);
            response.Contents = stream => stream.Write(body, 0, body.Length);

            return response;
        }

        public static HttpRequestBase AsNetRequest(this Request request)
        {
            HttpRequest req = new HttpRequest(request.Url.Path, request.Url.ToString(), request.Url.Query);

            req.RequestType = request.Method;
            var headers = req.Headers;
            foreach (var key in request.Headers.Keys)
            {
                headers.Add(key, request.Headers[key].ToString());
            }

            return new HttpRequestWrapper(req);
        }
    }
}

