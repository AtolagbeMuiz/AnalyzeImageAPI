using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DetectFacesAPI.Models;
using DetectFacesAPI.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using AnalyzeImageAPI.Models;
using AnalyzeImageAPI.Helpers;

namespace DetectFacesAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/{version:apiVersion}/[controller]")]
    [ApiController]
    public class AnalyzeImageController : ControllerBase
    {
        private readonly IAnalyzeRepository analyzeRepository;
        private readonly IConfiguration config;
        private readonly Helpers _helper;


        public AnalyzeImageController(IAnalyzeRepository analyzeRepository, IConfiguration config, Helpers helper)
        {
            this.analyzeRepository = analyzeRepository;
            this.config = config;
            this._helper = helper;

        }

        //[HttpPost("ImageURL")]
        //public async Task<ImageAnalysis> AnalyzeImage([FromQuery] string imageFilePath, string email)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        BadRequest();
        //    }

        //    Image image = new Image
        //    {
        //        ImageUrl = imageFilePath
        //    };


        //    await analyzeRepository.Save(image);

        //    var subscriptionKey = config.GetSection("API:ComputerVisionAPI:subscriptionKey").Value;
        //    var endpoint = config.GetSection("API:ComputerVisionAPI:endpoint").Value;

        //    ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);

        //    ImageAnalysis imageAnalysis = await AnalyzeImageUrl(client, imageFilePath);

        //    Email emailObj = new Email(config);
        //    emailObj.Execute(email).Wait();

        //    return imageAnalysis;

        //}
        [HttpPost("ImageURL")]
        public async Task<ImageAnalysis> AnalyzeImage([FromQuery] string imageFilePath, string email)
        {
            if (!ModelState.IsValid)
            {
                 BadRequest();
            }

            try
            {
                Image image = new Image
                {
                    ImageUrl = imageFilePath
                };
                await analyzeRepository.Save(image);

                var subscriptionKey = config.GetSection("API:ComputerVisionAPI:subscriptionKey").Value;
                var endpoint = config.GetSection("API:ComputerVisionAPI:endpoint").Value;
                ComputerVisionClient client = Authenticate(endpoint, subscriptionKey);

                ImageAnalysis imageAnalysis;

                // Check if the path is a URL or local file path
                if (_helper.IsUrl(imageFilePath))
                {
                    // Analyze image from URL
                    imageAnalysis = await AnalyzeImageUrl(client, imageFilePath);
                }
                else
                {
                    // Analyze image from local file directory
                    imageAnalysis = await AnalyzeImageFromLocalFile(client, imageFilePath);
                }

                Email emailObj = new Email(config);
                await emailObj.Execute(email);

                return imageAnalysis;
            }
            catch (FileNotFoundException)
            {
                BadRequest("Image file not found at the specified path.");
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                BadRequest("Access denied to the specified file path.");
                throw;
            }
            catch (Exception ex)
            {
                BadRequest($"Error analyzing image: {ex.Message}");
                throw;
            }
        }

       
        // New method to analyze images from local file system
        private async Task<ImageAnalysis> AnalyzeImageFromLocalFile(ComputerVisionClient client, string localImagePath)
        {
            // Validate file exists
            if (!System.IO.File.Exists(localImagePath))
            {
                throw new FileNotFoundException($"Image file not found: {localImagePath}");
            }

            // Validate file extension
            string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            string fileExtension = Path.GetExtension(localImagePath).ToLower();

            if (!supportedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException($"Unsupported file format: {fileExtension}");
            }

            // Read the image file as a stream
            using (FileStream imageStream = System.IO.File.OpenRead(localImagePath))
            {
                // Define features to extract during image analysis
                List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
                {
                    VisualFeatureTypes.Categories,
                    VisualFeatureTypes.Description,
                    VisualFeatureTypes.Faces,
                    VisualFeatureTypes.ImageType,
                    VisualFeatureTypes.Tags,
                    VisualFeatureTypes.Adult,
                    VisualFeatureTypes.Color,
                    VisualFeatureTypes.Brands,
                    VisualFeatureTypes.Objects
                };

                // Analyze the image
                ImageAnalysis results = await client.AnalyzeImageInStreamAsync(imageStream, features);
                return results;
            }
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
                new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
                { Endpoint = endpoint };
            return client;
        }

        // Gets the analysis of the specified image by using the Face REST API.
        public static async Task<ImageAnalysis> AnalyzeImageUrl(ComputerVisionClient client, string imageUrl)
        {
            // Creating a list that defines the features to be extracted from the image. 
            List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
                {
                    VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
                    VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
                    VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
                    VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
                    VisualFeatureTypes.Objects
                };

            ImageAnalysis results = await client.AnalyzeImageAsync(imageUrl, features);

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(@"C:\Users\AnalyzedImage.txt"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, results);
               
            }

            return results;

           

        }

       


    }
       
    }
