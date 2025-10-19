using Amazon.S3.Model;
using Amazon.S3;
using Moq;
using System.Text;
using AWSLambda3_S3Function;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.S3Events;

namespace AWSLambda3_S3FunctionTests;

[TestFixture]
public class FunctionS3Tests
{
    private Mock<IAmazonS3> _s3Mock;
        private Mock<IDynamoDBContext> _dynamoDbMock;
        private Mock<ILambdaContext> _lambdaContextMock;
        private Function _function;

        [SetUp]
        public void Setup()
        {
            _s3Mock = new Mock<IAmazonS3>();
            _dynamoDbMock = new Mock<IDynamoDBContext>();
            _lambdaContextMock = new Mock<ILambdaContext>();

        // Mock the logger inside ILambdaContext
        var loggerMock = new Mock<ILambdaLogger>();
        _lambdaContextMock.SetupGet(c => c.Logger).Returns(loggerMock.Object);

        // Inject mocks into Function using constructor overloading
        _function = new FunctionForTest(_s3Mock.Object, _dynamoDbMock.Object);
        }

        [Test]
        public async Task FunctionHandler_Should_SaveUsers_ToDynamoDB()
        {
            // Arrange: prepare fake Excel file in memory
            var excelStream = new MemoryStream();
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.AddWorksheet("Users");
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Age";

                worksheet.Cell(2, 1).Value = 5;
                worksheet.Cell(2, 2).Value = "John";
                worksheet.Cell(2, 3).Value = 30;

                workbook.SaveAs(excelStream);
            }
            excelStream.Position = 0;

            _s3Mock.Setup(x => x.GetObjectAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetObjectResponse
                {
                    ResponseStream = excelStream
                });

            var s3Event = new S3Event
            {
                Records = new List<S3Event.S3EventNotificationRecord>
                {
                    new S3Event.S3EventNotificationRecord
                    {
                        S3 = new S3Event.S3Entity
                        {
                            Bucket = new S3Event.S3BucketEntity { Name = "test-bucket" },
                            Object = new S3Event.S3ObjectEntity { Key = "test.xlsx" }
                        }
                    }
                }
            };

            // Act
            await _function.FunctionHandler(s3Event, _lambdaContextMock.Object);

            // Assert: verify that DynamoDB SaveAsync was called with the right User
            _dynamoDbMock.Verify(db => db.SaveAsync(
                It.Is<User>(u => u.Id == 5 && u.Name == "John" && u.Age == 30),
                default),
                Times.Once);
        }

}

/// <summary>
/// Custom subclass to inject mocks since original Function only allows S3 in ctor.
/// </summary>
public class FunctionForTest : Function
{
    public FunctionForTest(IAmazonS3 s3Client, IDynamoDBContext dynamoDBContext)
    {
        base.S3Client = s3Client;
        base.GetType()
            .GetField("_dynamoDBContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(this, dynamoDBContext);
    }
}