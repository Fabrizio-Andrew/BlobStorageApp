namespace BlobStorageApp.Settings
{
    /// <summary>
    /// Implements the picture settings 
    /// </summary>
    public class PictureSettings : IPictureSettings
    {
        /// <summary>
        /// The picures storage container name
        /// </summary>
        public string PictureContainerName { get; set; }
    }
}
