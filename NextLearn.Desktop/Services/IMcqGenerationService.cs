using System.Threading.Tasks;

namespace NextLearn.Desktop.Services;

public interface IMcqGenerationService
{
    Task<McqGenerationResult> GenerateMcqAsync(string deckContent, string apiKey, int questionCount = 5);
}
