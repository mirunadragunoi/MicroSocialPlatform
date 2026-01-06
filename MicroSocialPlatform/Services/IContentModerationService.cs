namespace MicroSocialPlatform.Services
{
    /// <summary>
    /// Interface pentru serviciul de moderare a conținutului folosind AI
    /// </summary>
    public interface IContentModerationService
    {
        /// <summary>
        /// Verifică dacă un text conține conținut neadecvat
        /// </summary>
        /// <param name="text">Textul de verificat</param>
        /// <returns>Rezultatul moderării (IsClean, Reason)</returns>
        Task<ModerationResult> ModerateContentAsync(string text);
    }

    /// <summary>
    /// Rezultatul analizei de moderare
    /// </summary>
    public class ModerationResult
    {
        /// <summary>
        /// Indică dacă textul este curat (fără conținut neadecvat)
        /// </summary>
        public bool IsClean { get; set; }

        /// <summary>
        /// Motivul blocării (dacă IsClean = false)
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Lista de categorii problematice detectate
        /// </summary>
        public List<string> DetectedIssues { get; set; } = new List<string>();

        /// <summary>
        /// Nivel de confidență (0.0 - 1.0)
        /// </summary>
        public double Confidence { get; set; }
    }
}