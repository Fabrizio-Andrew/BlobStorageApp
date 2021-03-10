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
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
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

            // Validate client-submitted parameters before sending request to Azure.
            List<ErrorResponse> payloadValidation = ValidatePayload(containerName, fileName, formFile);
            if (payloadValidation.Count > 0) {
                return BadRequest(payloadValidation);
            }
            try {
                // Create or overwrite the blob with contents of the message provided
                using Stream stream = formFile.OpenReadStream();
                await _storageRepository.UploadFile(containerName, fileName, stream, formFile.ContentType);

                return CreatedAtRoute("GetFileByIdRoute", new { containerName = containerName, fileName = fileName }, null);
            }

            // Catch Azure Exceptions
            catch (Exception ex)
            {
                if (ex.Message.Contains("InvalidResourceName")) {
                    return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(null, ex.Message, "containerName", containerName));
                }
                else if (ex.Message.Contains("BlobNotFound")) {
                    return StatusCode((int)HttpStatusCode.NotFound, ErrorResponse.GenerateErrorResponse(4, null, "fileName", fileName));
                }
                return BadRequest();
            }

        }

        /// <summary>
        /// Updates/Overwrites an existing file within a container.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="fileName"></param>
        /// <param name="formFile">The updated file.</param>
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Route("/api/v1/{containerName}/contentfiles/{fileName}")]
        [HttpPatch]
        public async Task<IActionResult> UpdateFile([FromRoute]string containerName, [FromRoute]string fileName, IFormFile formFile)
        {
            // Validate client-submitted parameters before sending request to Azure.
            List<ErrorResponse> payloadValidation = ValidatePayload(containerName, fileName, formFile);
            if (payloadValidation.Count > 0) {
                return BadRequest(payloadValidation);
            }

            try
            {
                // Get the existing file by containerName & fileName
                (MemoryStream memoryStream, string contentType) = await _storageRepository.GetFileAsync(containerName, fileName);
            }

            // Catch Azure Exceptions
            catch (Exception ex)
            {
                if (ex.Message.Contains("InvalidResourceName")) {
                    return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(null, ex.Message, "containerName", containerName));
                }
                else if (ex.Message.Contains("BlobNotFound")) {
                    return StatusCode((int)HttpStatusCode.NotFound, ErrorResponse.GenerateErrorResponse(4, null, "fileName", fileName));
                }
                return BadRequest();
            }

            // Overwrite the blob with contents of the file provided
            using Stream stream = formFile.OpenReadStream();
            await _storageRepository.UploadFile(containerName, fileName, stream, formFile.ContentType);

            return NoContent();
        }

        /// <summary>
        /// Deletes an existing file within a container.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="fileName"></param>
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Route("/api/v1/{containerName}/contentfiles/{fileName}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteFile([FromRoute] string containerName, [FromRoute] string fileName)
        {
            // Validate client-submitted parameters before sending request to Azure.
            List<ErrorResponse> payloadValidation = ValidatePayload(containerName, fileName);
            if (payloadValidation.Count > 0) {
                return BadRequest(payloadValidation);
            }

            try
            {
                // Get the existing file by containerName & fileName
                (MemoryStream memoryStream, string contentType) = await _storageRepository.GetFileAsync(containerName, fileName);
            }

            // Catch Azure Exceptions
            catch (Exception ex)
            {
                if (ex.Message.Contains("InvalidResourceName")) {
                    return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(null, ex.Message, "containerName", containerName));
                }
                else if (ex.Message.Contains("BlobNotFound")) {
                    return StatusCode((int)HttpStatusCode.NotFound, ErrorResponse.GenerateErrorResponse(4, null, "fileName", fileName));
                }
                return BadRequest();
            }

            // Delete the file
            await _storageRepository.DeleteFile(containerName, fileName);
            return NoContent();
        }


        /// <summary>
        /// Returns a list of all available files in the container provided.
        /// </summary>
        /// <returns>A list of available files within the container.</returns>
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Route("/api/v1{containerName}/contentfiles")]
        [HttpGet]
        public async Task<IActionResult> GetContainerFiles([FromRoute]string containerName)
        {

            // Validate client-submitted parameters before sending request to Azure.
            List<ErrorResponse> payloadValidation = ValidatePayload(containerName);
            if (payloadValidation.Count > 0) {
                return BadRequest(payloadValidation);
            }

            try {
                // Get only the blobs within the specified container
                List<string> blobList = await _storageRepository.GetListOfBlobs(containerName); 
                return new ObjectResult(blobList.ToArray());
            }

            // Catch Azure Exceptions
            catch (Exception ex)
            {
                if (ex.Message.Contains("InvalidResourceName")) {
                    return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(null, ex.Message, "containerName", containerName));
                }
                return BadRequest();
            }
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

            // Validate client-submitted parameters before sending request to Azure.
            List<ErrorResponse> payloadValidation = ValidatePayload(containerName, fileName);
            if (payloadValidation.Count > 0) {
                return BadRequest(payloadValidation);
            }
    
            try {
                // Get the file by containerName and fileName
                (MemoryStream memoryStream, string contentType) = await _storageRepository.GetFileAsync(containerName, fileName);
                return File(memoryStream, contentType);
            }

            // Catch Azure Exceptions
            catch (Exception ex)
            {
                if (ex.Message.Contains("InvalidResourceName")) {
                    return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(null, ex.Message, "containerName", containerName));
                }
                return BadRequest();
            }
        }

        /// <summary>
        /// Validates the input parameters before sending to Azure Storage.
        /// </summary>
        /// <param name="containerName">The containerName provided by client</param>
        /// <param name="fileName">The fileName provided by client (Optional)</param>
        /// <param name="fileData">The fileData provided by client (Optional)</param>
        /// <returns>A List of ErrorResponses</returns>
        public static List<ErrorResponse> ValidatePayload(string? containerName, string? fileName = "placeholder999", params IFormFile[] fileData) {
            
            // Empty list to collect Error Responses
            List<ErrorResponse> errorResponses = new List<ErrorResponse>();

            // Validate containerName rules
            if (containerName.Length > 75) {
                ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(2, null, "containerName", containerName);
                errorResponses.Add(errorResponse);
            }
            if (containerName == null) {
                ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(6, null, "containerName", null);
                errorResponses.Add(errorResponse);
            }
            if (containerName == "") {
                ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(3, null, "containerName", containerName);
                errorResponses.Add(errorResponse);
            }

            // Validate fileName rules (if provided as an argument)
            if (fileName != "placeholder999") {
                
                if (fileName == null) {
                    ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(6, null, "fileName", null);
                    errorResponses.Add(errorResponse);
                }
                if (fileName == "") {
                    ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(3, null, "fileName", fileName);
                    errorResponses.Add(errorResponse);
                }
                if (fileName.Length > 63) {
                    ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(2, null, "fileName", fileName);
                    errorResponses.Add(errorResponse);
                }
                else if (fileName.Length < 3) {
                    ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(5, null, "fileName", fileName);
                    errorResponses.Add(errorResponse);
                }
            }

            // validate that fileName is not null (if provided as an argument)
            if (fileData.Length > 0) {
                foreach (IFormFile file in fileData) {
                    if (file == null) {
                        ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(3, null, "fileData", null);
                        errorResponses.Add(errorResponse);
                    }
                }
            }
            return errorResponses;

        }
    }
}
