using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.ElasticTranscoder;
using Amazon.ElasticTranscoder.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TFHVTranscodeVideo
{
    public class Function
    {

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            LambdaLogger.Log("Welcome");
            var etsClient = new AmazonElasticTranscoderClient(RegionEndpoint.USEast1);
            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                return null;
            }

            try
            {
                var key = s3Event.Object.Key;
                if (key == null)
                {
                    LambdaLogger.Log("Object key is null!");
                    return null;
                }
                var sourceKey = HttpUtility.UrlDecode(key.Replace(' ', '+'));
                
                if (sourceKey == null || (!sourceKey.EndsWith(".mp4") && !sourceKey.EndsWith(".avi") &&
                    !sourceKey.EndsWith(".mov")))
                {
                    LambdaLogger.Log("Not valid video format!");
                    return null;
                }
                LambdaLogger.Log("source key: " + sourceKey);
                var outputKey="";
                var sourceKeyStrings = sourceKey.Split('.');
                for (var i = 0; i < sourceKeyStrings.Length-1; i++)
                {
                    outputKey += sourceKeyStrings[i]; 
                }
                LambdaLogger.Log("output key: " + outputKey);
                var outputs = new List<CreateJobOutput>
                {
                    new CreateJobOutput()
                    {
                        Key = outputKey + "-1080p.mp4",
                        PresetId = "1351620000001-000001" //Generic 1080p
                    },
                    new CreateJobOutput()
                    {
                        Key = outputKey + "-720p.mp4",
                        PresetId = "1351620000001-000010" //Generic 720p
                    },
                    new CreateJobOutput()
                    {
                        Key = outputKey + "-web-720p.mp4",
                        PresetId = "1351620000001-100070" //Web Friendly 720p
                    }
                };
                var response = await etsClient.CreateJobAsync(new CreateJobRequest()
                {
                    PipelineId = "1522009373582-29i9ac",
                    Input = new JobInput()
                    {
                        Key = sourceKey
                    },
                    Outputs = outputs
                });

                //var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                return response.Job.Id;
            }
            catch (Exception e)
            {
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }
    }
}
