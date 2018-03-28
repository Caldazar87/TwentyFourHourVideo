using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.S3Events;
using Amazon.Runtime.Internal.Auth;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TFHVTranscodedBucketACL
{
    public class Function
    {
        private IAmazonS3 S3Client { get; set; }

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
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(SNSEvent evnt, ILambdaContext context)
        {
            LambdaLogger.Log("Welcome");

            //TODO fix parsing of Json message
            JObject snsMessage = JObject.Parse(evnt.Records[0].Sns.Message);
            if (snsMessage == null)
            {
                LambdaLogger.Log("SNS message is null!");
                return null;
            }

            try
            {
                LambdaLogger.Log(snsMessage.ToString());
                var sourceBucket = snsMessage["Records"][0]["s3"]["bucket"]["name"].ToString();
                LambdaLogger.Log(sourceBucket);
                var sourceKey = HttpUtility.UrlDecode(snsMessage["Records"][0]["s3"]["object"]["key"].ToString().Replace(' ', '+'));
                LambdaLogger.Log(sourceKey);
                //// Retrieve ACL for object
                //GetACLResponse r = await S3Client.GetACLAsync(new GetACLRequest
                //{
                //    BucketName = sourceBucket
                //});
                //S3AccessControlList oldAcl = r.AccessControlList;
                //// Retrieve owner
                //Owner owner = oldAcl.Owner;

                //S3Grant grant = new S3Grant()
                //{
                //    Permission = S3Permission.READ,
                //    Grantee = new S3Grantee()
                //    {
                //        URI = "http://acs.amazonaws.com/groups/global/AllUsers"
                //    }
                //};
                //S3AccessControlList newAcl = new S3AccessControlList
                //{
                //    Grants = new List<S3Grant> { grant },
                //    Owner =  owner
                //};

                // Set new ACL.
                PutACLResponse response = await S3Client.PutACLAsync(new PutACLRequest()
                {
                    BucketName = sourceBucket,
                    Key = sourceKey,
                    CannedACL = S3CannedACL.PublicRead

                });



                return response.ToString();
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
