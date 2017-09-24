using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace WorkoutTrackerFuncApp
{
    public static class GetWorkoutSessions
    {
        [FunctionName("GetWorkoutSessions")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string name = GetUserId(req, log);

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            name = name ?? data?.name;

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
        }

        // Need to make this more secure in the future. Need to get the value from the JWT token. 
        // Header value can be modified
        //
        public static string GetUserId(HttpRequestMessage req, TraceWriter log)
        {
            string id = req.Headers.GetValues("X-MS-CLIENT-PRINCIPAL-ID").FirstOrDefault();

            log.Info("X-MS-CLIENT-PRINCIPAL-ID:" + id);

            return id;
        }
    }
}
