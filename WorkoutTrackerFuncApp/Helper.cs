using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;

namespace WorkoutTrackerFuncApp
{
    public static class Helper
    {
        

        public static SqlParameter getPrincipalId(HttpRequestMessage req, TraceWriter log)
        {
            var principalId = Helper.GetUserId(req, log);
            return new SqlParameter(Constants.SQL_PRINCIPAL_ID, principalId);
        }

        // Need to make this more secure in the future. Need to get the value from the JWT token. 
        // Header value can be modified
        //
        public static string GetUserId(HttpRequestMessage req, TraceWriter log)
        {
            log.Info("GetUserId started");
            IEnumerable<string> idList = null;
            string id = null;

            if (req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-ID", out idList))
            {
                id = idList.FirstOrDefault();
                if (string.IsNullOrEmpty(id))
                {
                    log.Info("X-MS-CLIENT-PRINCIPAL-ID is null");
                    throw new NullReferenceException("Principal id");
                }
            }
            else
            {
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Delete this in the future once auth is working !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                log.Info("X-MS-CLIENT-PRINCIPAL-ID is null");
                id = "noauthsetupyetId";
            }

            log.Info("X-MS-CLIENT-PRINCIPAL-ID:" + id);

            return id;
        }

        public delegate object GenerateReturnObject(SqlDataReader reader);

        public static async Task<HttpResponseMessage> GetSqlHelper(
            HttpRequestMessage req,
            TraceWriter log,
            bool isList,
            String query,
            SqlParameter[] parameters,
            GenerateReturnObject returnObject)
        {
            try
            {
                log.Info("GetSqlHelper started");
                var connectionString = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    log.Error($"SQL connection string");
                    throw new ArgumentNullException("SQL connection string");
                }

                List<object> results = new List<object>();
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(Helper.getPrincipalId(req, log));
                        if(parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        
                        var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            results.Add(returnObject(reader));
                        }
                    }
                    con.Close();
                }

                return GenerateGetHttpResponseMessage(isList, results);
            }
            catch (Exception ex)
            {
                log.Error($"C# Http trigger function exception: {ex.Message}");
                return new HttpResponseMessage() { Content = new StringContent(""), StatusCode = HttpStatusCode.InternalServerError };
            }
        }

        public static HttpResponseMessage GenerateGetHttpResponseMessage(bool isList, List<object> results)
        {
            string body = null;
            if (isList)
            {
                //Since it is a list just return an empty array if there is no results
                if (results == null || !results.Any())
                {
                    body = "[]";
                }
                else
                {
                    body = Newtonsoft.Json.JsonConvert.SerializeObject(results);
                }
            }
            else
            {
                if (results == null || !results.Any())
                {
                    return new HttpResponseMessage() { Content = null, StatusCode = HttpStatusCode.NotFound };
                }
                else if (results.Count == 1)
                {
                    body = Newtonsoft.Json.JsonConvert.SerializeObject(results.First());
                }
            }

            return new HttpResponseMessage() { Content = new StringContent(body), StatusCode = HttpStatusCode.OK };
        }
    }
}
