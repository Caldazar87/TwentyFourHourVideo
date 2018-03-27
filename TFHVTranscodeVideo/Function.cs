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
        IAmazonS3 S3Client { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
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
                var sourceKey = HttpUtility.UrlDecode(key.Replace(' ', '+'));
                var outputKey = sourceKey.Split('.')[0];
                var outputs = new List<CreateJobOutput>();
                outputs.Add(new CreateJobOutput()
                {
                    Key = outputKey + "-1080p.mp4",
                    PresetId = "1351620000001-000001" //Generic 1080p
                });
                outputs.Add(new CreateJobOutput()
                {
                    Key = outputKey + "-720p.mp4",
                    PresetId = "1351620000001-000010" //Generic 720p
                });
                outputs.Add(new CreateJobOutput()
                {
                    Key = outputKey + "-web-720p.mp4",
                    PresetId = "1351620000001-100070" ///Web Friendly 720p
                });
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
                context.Logger.LogLine($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }
    }
}
