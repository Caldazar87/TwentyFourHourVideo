using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON evnt to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TFHVUserProfile
{
    public class Function
    {

        public async Task<string> FunctionHandler(APIGatewayCustomAuthorizerRequest evnt, ILambdaContext context)
        {
            if (String.IsNullOrEmpty(evnt.AuthorizationToken))
            {
                LambdaLogger.Log("No authorization token received!");
                return null;
            }
            LambdaLogger.Log(evnt.AuthorizationToken);
            var token = evnt.AuthorizationToken.Split(' ')[1];
            LambdaLogger.Log(token);
            var secretBuffer =  Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("AUTH0_SECRET"));
            LambdaLogger.Log(secretBuffer.ToString());

            return evnt.AuthorizationToken;
        }
    }
}
