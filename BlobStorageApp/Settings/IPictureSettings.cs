namespace BlobStorageApp.Settings
{
    /// <summary>
    /// Defines the picture settings 
    /// </summary>
    public interface IPictureSettings
    {
        /// <summary>
        /// The picures storage container name
        /// </summary>
        public string PictureContainerName { get; set; }
    }
}
