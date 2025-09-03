using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

namespace UploadBunchOfFile.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost("upload-to-s3")]
        public async Task<IActionResult> UploadToS3(List<IFormFile> files)
        {
            var s3Client = new AmazonS3Client("", "", "");
            var bucketName = "";

            await Parallel.ForEachAsync(files,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (file, ct) =>
                {
                    var key = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                    using var stream = file.OpenReadStream();
                    var request = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = file.ContentType
                    };

                    await s3Client.PutObjectAsync(request, ct);
                });
            return Ok(new { Count = files.Count, Message = "Files uploaded to AWS S3" });
        }

    }
}
