using IdCard.Domain.Models;

namespace IdCard.Domain.Interfaces;

public interface IIdCardRenderer
{
    /// <summary>
    /// Renders front and back images.
    /// Front is drawn from the template; back is returned as raw bytes from the asset file.
    /// </summary>
    Task<IdCardResult> RenderAsync(string templatePath, IdCardContext context);
}
