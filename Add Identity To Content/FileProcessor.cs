namespace AddIdentityToContent
{
    /// <summary>
    /// Provides methods to process files and categorize them based on their extensions.
    /// </summary>
    /// <remarks>
    /// Supported image file extensions: .jpg, .jpeg, .png, .webp, .bmp, .tiff, .tif, .ico, .svg, .heic, .heif, .raw, .cr2, .nef, .orf, .sr2, .arw, .dng, .raf, .rw2, .pef, .x3f, .3fr, .erf, .kdc, .mrw, .nrw, .ptx, .r3d, .srw
    /// Supported video file extensions: .mp4, .avi, .mpeg, .mpg, .webm, .mov, .mkv, .flv, .wmv, .3gp, .3g2, .m4v, .f4v, .f4p, .f4a, .f4b, .vob, .ogv, .ogg, .drc, .mng, .mts, .m2ts, .ts, .rm, .rmvb, .asf, .amv, .m2v, .svi, .3gpp, .3gpp2
    /// </remarks>
    internal static class FileProcessor
    {
        // Supported image file extensions
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>
            {
                ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tiff", ".tif", ".ico", ".svg", ".heic", ".heif", ".raw", ".cr2", ".nef", ".orf", ".sr2", ".arw", ".dng", ".raf", ".rw2", ".pef", ".x3f", ".3fr", ".erf", ".kdc", ".mrw", ".nrw", ".ptx", ".r3d", ".srw"
            };

        // Supported video file extensions
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>
            {
                ".mp4", ".avi", ".mpeg", ".mpg", ".webm", ".mov", ".mkv", ".flv", ".wmv", ".3gp", ".3g2", ".m4v", ".f4v", ".f4p", ".f4a", ".f4b", ".vob", ".ogv", ".ogg", ".drc", ".mng", ".mts", ".m2ts", ".ts", ".rm", ".rmvb", ".asf", ".amv", ".m2v", ".svi", ".3gpp", ".3gpp2"
            };

        /// <summary>
        /// Processes the given files and categorizes them into image, video, and gif files.
        /// Unsupported file formats are logged.
        /// </summary>
        /// <param name="droppedObjects">The collection of dropped objects to process.</param>
        /// <param name="logFilePath">The path to the log file where unsupported formats are logged.</param>
        /// <param name="imageFiles">The list to store image files.</param>
        /// <param name="videoFiles">The list to store video files.</param>
        /// <param name="gifFiles">The list to store gif files.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task ProcessFilesAsync(IEnumerable<DroppedObject> droppedObjects, string logFilePath, List<DroppedObject> imageFiles, List<DroppedObject> videoFiles, List<DroppedObject> gifFiles)
        {
            // Create a new log file or append to an existing one
            using (StreamWriter logFile = new StreamWriter(logFilePath, true))
            {
                // Iterate through each dropped object and categorize them based on their extensions
                foreach (var obj in droppedObjects)
                {
                    // Get the file extension and convert it to lowercase
                    string extension = Path.GetExtension(obj.Name ?? string.Empty).ToLower();

                    // Categorize the file based on its extension
                    if (ImageExtensions.Contains(extension))
                    {
                        imageFiles.Add(obj);
                    }
                    else if (VideoExtensions.Contains(extension))
                    {
                        videoFiles.Add(obj);
                    }
                    else if (extension == ".gif")
                    {
                        gifFiles.Add(obj);
                    }
                    else
                    {
                        // Log unsupported file formats
                        await logFile.WriteLineAsync($"Unsupported file format: {obj.FullPath}");
                    }
                }
            }
        }
    }
}
