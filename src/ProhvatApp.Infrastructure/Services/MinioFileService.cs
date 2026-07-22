using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using ProhvatApp.Application.Common.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ProhvatApp.Infrastructure.Services;

public class MinioFileService : IFileService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _endpointUrl;

    public MinioFileService(IConfiguration configuration)
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = configuration["S3Storage:Endpoint"],
            ForcePathStyle = true // Required for MinIO
        };

        _s3Client = new AmazonS3Client(
            configuration["S3Storage:AccessKey"],
            configuration["S3Storage:SecretKey"],
            s3Config
        );

        _bucketName = configuration["S3Storage:BucketName"] ?? "prohvat";
        _endpointUrl = configuration["S3Storage:PublicEndpoint"] ?? configuration["S3Storage:Endpoint"] ?? "http://localhost:9000";
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        // Check if bucket exists, if not, create it
        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
        if (!bucketExists)
        {
            var putBucketRequest = new Amazon.S3.Model.PutBucketRequest
            {
                BucketName = _bucketName,
                UseClientRegion = true
            };
            await _s3Client.PutBucketAsync(putBucketRequest);
            
            // Set bucket policy for public read access
            var policy = $@"{{
              ""Version"": ""2012-10-17"",
              ""Statement"": [
                {{
                  ""Action"": [""s3:GetObject""],
                  ""Effect"": ""Allow"",
                  ""Principal"": {{""AWS"": [""*""]}},
                  ""Resource"": [""arn:aws:s3:::{_bucketName}/*""]
                }}
              ]
            }}";
            // We set policy manually because CannedACL might not be enough depending on MinIO config
            await _s3Client.PutBucketPolicyAsync(new Amazon.S3.Model.PutBucketPolicyRequest
            {
                BucketName = _bucketName,
                Policy = policy
            });
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = uniqueFileName,
            BucketName = _bucketName,
            ContentType = contentType
        };

        var transferUtility = new TransferUtility(_s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        // Return the public URL to access the file
        return $"{_endpointUrl}/{_bucketName}/{uniqueFileName}";
    }
}
