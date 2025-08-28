using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda1;

public class Function
{
    
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<User?> FunctionHandler(Int32 input, ILambdaContext context)
    {
        //return input?.Name?.ToUpper();
        //var dynamoDBContext = new DynamoDBContext(new AmazonDynamoDBClient());

        //// Load the User item with the given Id (input)
        //var user = await dynamoDBContext.LoadAsync<User>(input);

        //return user;
        // Use the recommended constructor for DynamoDBContext
        using var client = new AmazonDynamoDBClient();
        var dynamoDBContext = new DynamoDBContext(client);

        var user = await dynamoDBContext.LoadAsync<User>(input);

        return user;
    }
}
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
}
