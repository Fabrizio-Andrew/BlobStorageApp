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
        public string StorageConnectionString
        {
            get
            {
                return Configuration.GetConnectionString("DefaultConnection");
            }
        }

        // PUT api/<ContentFilesController>/5
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(List<ErrorResponse>), (int)HttpStatusCode.BadRequest)]
        [Route("/api/v1/{containerName}/contentfiles/{fileName}")]
        [HttpPut("{noteName}")]
        public async Task<IActionResult> UploadFile([FromRoute]string containerName, [FromRoute] string fileName, [FromBody]IFormFile fileData)
        {
            // Get the Cloud Storage Account
            CloudStorageAccount Account = CloudStorageAccount.Parse(StorageConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = Account.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync();

            if (containerName.ToLower().Contains("public")) {

                // Set permissions on the blob container to ALLOW public access
                await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }

            // IS BLOB STORAGE PRIVATE BY DEFAULT?

            // Retrieve reference to a blob named the blob specified by the caller
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

            // Create or overwrite the blob with contents of the message provided
            using Stream stream = fileData.OpenReadStream();
            await _storageRepository.UploadFile(fileData.FileName, stream, fileData.ContentType);

            return CreatedAtRoute("GetFileByIdRoute", null);
        }

        // GET: api/<ContentFilesController>
        [Route("{id}", Name = "GetFileByIdRoute")]
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ContentFilesController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ContentFilesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ContentFilesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ContentFilesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
