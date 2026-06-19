using Avalonia.Media.Imaging;

namespace VNO.Client.Services;

/// <summary>
/// Loads character poses and scene backgrounds from the data folder
/// </summary>
/// <remarks>
/// The legacy client drew the stage from data\characters\&lt;name&gt;, reading
/// char.ini for the show name and Default.ini [Emotions] for the pose list, with
/// each pose a full viewport png and a matching buttons thumbnail. Backgrounds
/// come from data\background. Missing files are tolerated so a partial character
/// still loads
/// </remarks>
public interface ICharacterAssetService
{
    /// <summary>
    /// Loads a character by its folder name, null when the folder is missing
    /// </summary>
    LoadedCharacter? LoadCharacter(string folderName);

    /// <summary>
    /// Loads a scene background image by name, null when it is missing
    /// </summary>
    Bitmap? LoadBackground(string name);

    /// <summary>
    /// Loads the first available background as a default scene, null when none
    /// </summary>
    Bitmap? LoadDefaultBackground();
}
