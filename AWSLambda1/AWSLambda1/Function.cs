using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda1;

public class Function
{
    private readonly IDynamoDBContext _dynamoDBContext;

    public Function()
    {
        _dynamoDBContext = new DynamoDBContext(new AmazonDynamoDBClient());
    }
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayHttpApiV2ProxyResponse?> FunctionHandler(
        APIGatewayHttpApiV2ProxyRequest request, 
        ILambdaContext context
        )
    {
        return request.RequestContext.Http.Method.ToUpper() switch
        {
            "GET" => await HandleGetRequest(request),
            "POST" => await HandlePostRequest(request),
        };
    }

    private async Task<APIGatewayHttpApiV2ProxyResponse?> HandlePostRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        var user = JsonSerializer.Deserialize<User>(request.Body);// ?? string.Empty);
        if (user == null)
        {
            return BadRequest("Invalid user in body");
        }

        await _dynamoDBContext.SaveAsync(user);
        return new APIGatewayHttpApiV2ProxyResponse()
        {
            Body = JsonSerializer.Serialize(user),
            StatusCode = 200
        };
    }

    private static APIGatewayHttpApiV2ProxyResponse BadRequest(string message)
    {
        return new APIGatewayHttpApiV2ProxyResponse()
        {
            Body = message,
            StatusCode = 400
        };
    }

    private async Task<APIGatewayHttpApiV2ProxyResponse?> HandleGetRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        request.PathParameters.TryGetValue("userId", out var userId);
        if (userId == null || !int.TryParse(userId, out var input))
        {
            return null;
        }
        //using var client = new AmazonDynamoDBClient();
        

        var user = await _dynamoDBContext.LoadAsync<User>(input);
        if (user != null)
        {
            return new APIGatewayHttpApiV2ProxyResponse()
            {
                Body = JsonSerializer.Serialize(user),
                StatusCode = 200
            };
        }

        return BadRequest("Invalid user in body");
    }
}
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
}
