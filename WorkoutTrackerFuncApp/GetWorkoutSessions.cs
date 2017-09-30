using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Data.SqlClient;
using System.Configuration;
using System;
using System.Collections.Generic;

namespace WorkoutTrackerFuncApp
{
    public static class GetWorkoutSessions
    {
        public const string GetQuery = @"select 
                                ws.Id,
                                ws.Name, 
                                ws.StartTime, 
                                ws.EndTime 
                            from [WorkoutSessions] ws  
                            Inner Join [AppUsers] u on u.Id = ws.AppUserId
                            WHERE u.PrincipalId = " + Constants.SQL_PRINCIPAL_ID + ";";

        public static object createGetObject(SqlDataReader reader)
        {
            var s = new
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                StartDate = reader.GetDateTime(2),
                EndDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
            };

            return s;
        }

        [FunctionName("WorkoutSessions")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "WorkoutSessions")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP GetWorkoutSessions function.");
            return await Helper.GetSqlHelper(
                req: req,
                log: log,
                isList: true,
                query: GetQuery,
                parameters: null,
                returnObject: createGetObject);
        }
    }

    public static class GetWorkoutSession
    {
        public const string GetQuery = @"select 
                                ws.Id,
                                ws.Name, 
                                ws.StartTime, 
                                ws.EndTime 
                            from [WorkoutSessions] ws  
                            Inner Join [AppUsers] u on u.Id = ws.AppUserId
                            WHERE u.PrincipalId = " + Constants.SQL_PRINCIPAL_ID + @" and
                            ws.id = " + Constants.SQL_SESSION_ID + " ; ";

        [FunctionName("WorkoutSession")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "WorkoutSessions/{sessionId}")]HttpRequestMessage req, string sessionId, TraceWriter log)
        {
            log.Info("C# HTTP GetWorkoutSessions function contained a sessionId.");
            Guid parsedSessionId;
            if (!Guid.TryParse(sessionId, out parsedSessionId))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a valid session id.");
            }

            var sqlParamSessionId = new SqlParameter(Constants.SQL_SESSION_ID, parsedSessionId);
            var sqlParams = new SqlParameter[] { sqlParamSessionId };

            return await Helper.GetSqlHelper(
                req: req,
                log: log,
                isList: false,
                query: GetQuery,
                parameters: sqlParams,
                returnObject: GetWorkoutSessions.createGetObject);
        }
    }
}
