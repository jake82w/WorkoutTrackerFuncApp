using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Belgrade.SqlClient.SqlDb;
using Belgrade.SqlClient;
using System.Data.SqlClient;
using System.Configuration;
using System;

namespace WorkoutTrackerFuncApp
{
    public static class GetWorkoutSessions
    {
        [FunctionName("GetWorkoutSessions")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var query = @"select 
                                ws.Id, 
                                ws.Name, 
                                ws.StartTime, 
                                ws.EndTime 
                            from [WorkoutSessions] ws 
                            Inner Join [Users] u on u.Id = ws.UserId
                            WHERE u.PrincipalId = @principalId";

            return await GetSqlHelper(req, log, query);
        }

        // Need to make this more secure in the future. Need to get the value from the JWT token. 
        // Header value can be modified
        //
        public static string GetUserId(HttpRequestMessage req, TraceWriter log)
        {
            string id = req.Headers.GetValues("X-MS-CLIENT-PRINCIPAL-ID").FirstOrDefault();

            log.Info("X-MS-CLIENT-PRINCIPAL-ID:" + id);

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Delete this in the future once auth is working !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (string.IsNullOrEmpty(id))
            {
                log.Info("X-MS-CLIENT-PRINCIPAL-ID is null");
                id = "noauthsetupyetId";
            }

            return id;
        }

        public static async Task<HttpResponseMessage> GetSqlHelper(
            HttpRequestMessage req,
            TraceWriter log,
            string query)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    log.Error($"SQL connection string");
                    throw new ArgumentNullException("SQL connection string");
                }

                var httpStatus = HttpStatusCode.OK;
                SqlCommand cmd = new SqlCommand(connectionString);
                var principalId = GetUserId(req, log);
                cmd.Parameters.Add(new SqlParameter("@principalId", principalId));

                IQueryMapper map = new QueryMapper(connectionString);
                var a = await map.GetStringAsync(cmd);

                string body = await (new QueryMapper(connectionString)
                            .OnError(ex => { httpStatus = HttpStatusCode.InternalServerError; }))
                .GetStringAsync(query);

                return new HttpResponseMessage() { Content = new StringContent(body), StatusCode = httpStatus };
            }
            catch (Exception ex)
            {
                log.Error($"C# Http trigger function exception: {ex.Message}");
                return new HttpResponseMessage() { Content = new StringContent(""), StatusCode = HttpStatusCode.InternalServerError };
            }
        }
    }
}
