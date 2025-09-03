using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using ClosedXML.Excel;
using System.Text.Json;
using System.Text.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda3_S3Function;

public class Function
{
    /// <summary>
    /// The Amazon S3 client used to process S3 objects.
    /// </summary>
    IAmazonS3 S3Client { get; set; }
    private readonly IDynamoDBContext _dynamoDBContext;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
        _dynamoDBContext = new DynamoDBContext(new AmazonDynamoDBClient());
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client">The service client to access Amazon S3.</param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
        foreach (var record in eventRecords)
        {
            var s3Event = record.S3;
            if (s3Event == null)
            {
                continue;
            }

            try
            {
                using var response = await this.S3Client.GetObjectAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                using var stream = response.ResponseStream;
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                var rows = new List<Dictionary<string, object?>>();
                var headerRow = worksheet.FirstRowUsed();
                var headers = headerRow.Cells().Select(c => c.GetString()).ToList();

                foreach (var dataRow in worksheet.RowsUsed().Skip(1))
                {
                    var rowDict = new Dictionary<string, object?>();
                    var cells = dataRow.Cells().ToList();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        rowDict[headers[i]] = i < cells.Count ? cells[i].GetValue<string>() : null;
                    }
                    rows.Add(rowDict);
                }

                var json = JsonSerializer.Serialize(rows);
                context.Logger.LogInformation(json);
                var options = new JsonSerializerOptions
                {
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                var users = JsonSerializer.Deserialize<List<User>>(json, options);
                context.Logger.LogInformation("Amir");
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        //user.Id = Convert.ToInt32(user.Id);
                        //user.Age = Convert.ToInt32(user.Age);
                        context.Logger.LogInformation("User ID: "+ Convert.ToString(user.Id));
                        context.Logger.LogInformation(JsonSerializer.Serialize(user));
                        context.Logger.LogInformation("done");
                        await _dynamoDBContext.SaveAsync(user);
                        

                    }
                }
                
            }
            catch (Exception e)
            {
                context.Logger.LogError($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogError(e.Message);
                context.Logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
}
[DynamoDBTable("User")]
public class User
{
    [DynamoDBHashKey]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Id { get; set; }
    public string? Name { get; set; }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Age { get; set; }
}