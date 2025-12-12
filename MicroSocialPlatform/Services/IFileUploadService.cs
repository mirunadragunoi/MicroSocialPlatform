namespace MicroSocialPlatform.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file);

        void DeleteFile(string relativePath);
    }
}
