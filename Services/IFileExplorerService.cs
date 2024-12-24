namespace ProcessImagesWithImageSharpSixLabors.Services
{
    public interface IFileExplorerService
    {
        /// <summary>
        /// Save file to server
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<string> SaveAsync(IFormFile file);
    }
}
