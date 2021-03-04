﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobStorageApp.Exceptions;
using BlobStorageApp.Settings;
using Microsoft.AspNetCore.Http;

namespace BlobStorageApp.Repositories
{
    public class StorageRepository : IStorageRepository
    {
        private BlobContainerClient _blobContainerClient;
        private BlobServiceClient _blobServiceClient;
        private IStorageAccountSettings _storageAccountSettings;
        private IPictureSettings _pictureSettings;

        private bool IsInitialized { get; set; }

        /// <summary>
        /// Initializes this instance for use, this is not thread safe
        /// </summary>
        /// <returns>A task</returns>
        /// <remarks>This method is not thread safe</remarks>
        private void InitializeAsync()
        {
            if (!IsInitialized)
            {
                _blobServiceClient = new BlobServiceClient(_storageAccountSettings.StorageAccountConnectionString);

                _blobContainerClient = _blobServiceClient.GetBlobContainerClient(_pictureSettings.PictureContainerName);
                
                _blobContainerClient.CreateIfNotExists(publicAccessType: PublicAccessType.None);

                IsInitialized = true;
            }
        }

        /// <summary>
        /// The blob container client
        /// </summary>
        private BlobContainerClient GetBlobContainerClient()
        {
            if (!IsInitialized)
            {
                InitializeAsync();
            }
            return _blobContainerClient;
        }

        public StorageRepository(IStorageAccountSettings storageAccountSettings,
                                 IPictureSettings pictureSettings)
        {
            _storageAccountSettings = storageAccountSettings;
            _pictureSettings = pictureSettings;
        }


        /// <summary>
        /// Uploads file to blob storage
        /// </summary>
        /// <param name="fileName">The filename of the file to upload which will be used as the blobId</param>
        /// <param name="fileStream">The correspnding fileStream associated with the fileName</param>
        /// <param name="contentType">The content type of the blob to upload</param>
        public async Task UploadFile(string fileName, Stream fileStream, string contentType)
        {
            BlobClient blobClient = GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream);
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders() { ContentType = contentType });
        }

        /// <summary>
        /// Deletes file from blob storage
        /// </summary>
        /// <param name="fileName"></param>
        public async Task DeleteFile(string fileName)
        {
            var blob = GetBlobClient(fileName);
            await blob.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Gets the file from the blob storage
        /// </summary>
        /// <param name="fileName">The id of the blob to download</param>
        /// <returns>A memory stream, which must be disposed by the caller, that contains the downloaded blob</returns>
        public async Task<(MemoryStream fileStream, string contentType)> GetFileAsync(string fileName)
        {
            BlobClient blobClient = GetBlobClient(fileName);
            using BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

            
            // Caller is expected to dispose of the memory stream
            MemoryStream memoryStream = new MemoryStream();
            await blobDownloadInfo.Content.CopyToAsync(memoryStream);

            // Reset the stream to the beginning so readers don't have to
            memoryStream.Position = 0;
            return (memoryStream, blobDownloadInfo.ContentType);
        }

        /// <summary>
        /// Gets the file from the blob storage
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>A byte array containing the downloaded blob content</returns>
        /// <exception cref="InternalException">If the http status is anything other than 404</exception>
        /// <exception cref="NotFoundException">If the blob can't be found</exception>
        public async Task<byte[]> GetFileInByteArrayAsync(string fileName)
        {
            BlobClient blobClient = GetBlobClient(fileName);

            using BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();
            using MemoryStream memoryStream = new MemoryStream();

            Response response = await blobClient.DownloadToAsync(memoryStream);

            if (response.Status == StatusCodes.Status200OK)
            {
                return memoryStream.ToArray();
            }

            if (response.Status == StatusCodes.Status404NotFound)
            {
                throw new NotFoundException($"FileName: {fileName} ReasonPhrase: {response.ReasonPhrase} Attempt to download blob failed because it was not found");
            }

            throw new InternalException($"FileName: {fileName} ReasonPhrase: {response.ReasonPhrase} Attempt to download blob failed because it was not found");
        }

        /// <summary>
        /// Returns all of the blob names in a container
        /// </summary>
        /// <returns>All of the blob names in a container</returns>
        /// <remarks>This does not scale, for scalability usitlize the pagaing functionaltiy
        /// to page through the blobs in t</remarks>
        public async Task<List<string>> GetListOfBlobs()
        {
            BlobContainerClient blobContainerClient = GetBlobContainerClient();
            var blobs = blobContainerClient.GetBlobsAsync();


            List<string> blobNames = new List<string>();

            await foreach (var blobPage in blobs.AsPages())
            {
                foreach (var blobItem in blobPage.Values)
                {
                    blobNames.Add(blobItem.Name);
                }

            }

            return blobNames;
        }

        /// <summary>
        /// Gets the blob client associated with the blob specified in the fileName
        /// </summary>
        /// <param name="fileName">The file name which is the blob id</param>
        /// <returns>The corresponding BlobClient for the fileName, blob ID specified</returns>
        private BlobClient GetBlobClient(string fileName)
        {
            BlobContainerClient blobContainerClient = GetBlobContainerClient();

            return blobContainerClient.GetBlobClient(fileName);
        }
    }
}