using Microsoft.AspNetCore.Hosting;

namespace MicroSocialPlatform.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            // generez numele unic pentru fisier si salvez in folderul "uploads"
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/uploads/" + uniqueFileName; // returnez calea relativa pentru a fi folosita in aplicatie
        }

        // metoda pentru stergerea fisierelor de pe disc
        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            try
            {
                // relativePath vine ca "/uploads/imagine.jpg".
                // Trebuie sa scapam de primul "/" pentru a combina corect caile.
                string pathToFile = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));

                // Verificam daca fisierul exista inainte de a incerca sa-l stergem
                if (System.IO.File.Exists(pathToFile))
                {
                    System.IO.File.Delete(pathToFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nu am putut șterge fișierul: {ex.Message}");
            }
        }
    }
}
