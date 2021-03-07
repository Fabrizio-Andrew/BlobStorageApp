using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage.Blob;
using BlobStorageApp.Repositories;
using BlobStorageApp.DataTransferObjects;
using BlobStorageApp.Settings;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BlobStorageApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentFilesController : ControllerBase
    {

        private readonly IStorageRepository _storageRepository;

        /// <summary>
        /// Returns the list of files available for download
        /// </summary>
        /// <returns>The list of files available for download</returns>
        public ContentFilesController(IStorageRepository storageRepository)
        {
            _storageRepository = storageRepository;
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public IConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets the storage connection string.
        /// </summary>
        /// <value>
        /// The storage connection string.
        /// </value>
        //public string StorageConnectionString
        //{
          //  get
          //  {
          //      return Configuration.GetConnectionString("DefaultConnection");
          //  }
        //}

        /// <summary>
        /// Uploads a file, or overwrites a file if it already exists.
        /// </summary>
        /// <param name="formFile">The picture to upload</param>
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(List<ErrorResponse>), (int)HttpStatusCode.BadRequest)]
        [Route("/api/v1/{containerName}/contentfiles/{fileName}")]
        [HttpPut]
        public async Task<IActionResult> UploadFile([FromRoute]string containerName, [FromRoute]string fileName, IFormFile formFile)
        {
            // Get the Cloud Storage Account
            //CloudStorageAccount Account = CloudStorageAccount.Parse(StorageConnectionString);

            // Create the blob client.
            //CloudBlobClient blobClient = Account.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            //CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Create the container if it doesn't already exist.
            //await container.CreateIfNotExistsAsync();

            //if (containerName.ToLower().Contains("public")) {

                // Set permissions on the blob container to ALLOW public access
                //await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });
            //}
            //else
            //{
                // Set permissions on the blob container to PREVENT public access (private container)
                //await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off });
            //}

            // Retrieve reference to a blob named the blob specified by the caller
            //CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            // Create or overwrite the blob with contents of the message provided
            using Stream stream = formFile.OpenReadStream();
            await _storageRepository.UploadFile(containerName, fileName, stream, formFile.ContentType);

            return CreatedAtRoute("GetFileByIdRoute", new { id = formFile.Name }, null);
        }

        /// <summary>
        /// Returns a list of all available files in the container provided.
        /// </summary>
        /// <returns>A list of available files within the container.</returns>
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Route("/api/v1{containerName}/contentfiles")]
        [HttpGet]
        public async Task<IEnumerable<string>> GetContainerFiles([FromRoute]string containerName)
        {
            // TO-DO: GET ONLY THE BLOBS WITHIN THE PROVIDED CONTAINER
            return await _storageRepository.GetListOfBlobs(containerName);
        }

        /// <summary>
        /// Returns a file specified by the input containerName and fileName.
        /// </summary>
        /// <returns>A file Stream.</returns>        
        [ProducesResponseType(typeof(Stream), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Route("/api/v1/{containerName}/contentfiles/{fileName}", Name = "GetFileByIdRoute")]
        [HttpGet]
        public async Task<IActionResult> GetFileById([FromRoute]string containerName, [FromRoute]string fileName)
        {
            (MemoryStream memoryStream, string contentType) = await _storageRepository.GetFileAsync(containerName, fileName);
            return File(memoryStream, contentType);
        }
    }
}
