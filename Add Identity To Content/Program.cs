global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Threading.Tasks;
global using System.Linq;
global using System.Diagnostics;
//Xabe.FFmpeg and ImageMagick are not included in the .NET SDK, you need to install them using NuGet.
// The using directive for Xabe.FFmpeg does not highlight in Visual Studio, but it is still recognized.
global using Xabe.FFmpeg;
global using ImageMagick;
global using System.Text.RegularExpressions;

// Developer Notes:
// Converts dropped objects to either png image, an animated gif, or a video.
// Also embeds a watermark identity to the content.
// Gif files are exported to both formats, gif and mp4.
// Sometimes the program may not terminate ffmpeg processes on console close.
// Sometimes you may have to manually terminate ffmpeg processes in Task Manager.
// sometimes the gif conversion may only output the converted video,
// you will have to add them back through the process again to add the watermark.
// You need to download ImageMagick and FFmpeg to run this program.
// You need to have the watermark.png file in the same directory as the source files.
// The FFmpeg and ImageMagick paths must be placed in the same directory as the executable once it is built, GitHub does not allow uploading these files.
// The program will log errors to error.log and unsupported files to unsupported_files.log.
// Note: this project was built with the assistance of GitHub Copilot X Chat AI.

namespace AddIdentityToContent
{
    /// <summary>
    /// Main program class that processes dropped files, categorizes them, and adds watermarks.
    /// </summary>
    /// <remarks>
    /// Supported image file extensions: .jpg, .jpeg, .png, .webp, .bmp, .tiff, .tif, .ico, .svg, .heic, .heif, .raw, .cr2, .nef, .orf, .sr2, .arw, .dng, .raf, .rw2, .pef, .x3f, .3fr, .erf, .kdc, .mrw, .nrw, .ptx, .r3d, .srw
    /// Supported video file extensions: .mp4, .avi, .mpeg, .mpg, .webm, .mov, .mkv, .flv, .wmv, .3gp, .3g2, .m4v, .f4v, .f4p, .f4a, .f4b, .vob, .ogv, .ogg, .drc, .mng, .mts, .m2ts, .ts, .rm, .rmvb, .asf, .amv, .m2v, .svi, .3gpp, .3gpp2
    /// </remarks>
    /// <seealso cref="DroppedObject"/>
    /// <seealso cref="FileProcessor"/>
    /// <seealso cref="ImageProcessor"/>
    /// <seealso cref="Logger"/>
    /// <seealso cref="ImageMagick"/>
    /// <seealso cref="Xabe.FFmpeg"/>
    internal class Program
    {
        // Directory to store processed videos
        private static string videosOutputDirectory = "Videos";

        // Path to the log file for errors
        private static string logFilePath = "error.log";

        /// <summary>
        /// Main entry point of the program.
        /// Processes dropped files, categorizes them, and adds watermarks.
        /// </summary>
        /// <param name="args">Array of file paths to process.</param>
        static async Task Main(string[] args)
        {
            // Start the stopwatch to measure the duration of the entire process
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Handle console close event to terminate ffmpeg processes
            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnConsoleExit);

            try
            {
                // Specify the directory to scan for files relative to the executable
                string directoryPath = "./";

                // Load the watermark image
                string watermarkPath = Path.Combine(directoryPath, "watermark.png");
                // Check if the watermark file exists
                if (!File.Exists(watermarkPath))
                {
                    Console.WriteLine("Watermark file not found. Should be in the same folder as source files");
                    // Wait for input before exiting
                    Console.WriteLine("Press enter to finish");
                    Console.ReadLine();
                    // End the program and exit
                    return;
                }

                // Create a collection to store dropped objects
                List<DroppedObject> droppedObjects = new List<DroppedObject>();

                // Add files from the args array to the droppedObjects collection
                int idCounter = 1;
                // Loop through each file in the args array
                foreach (var file in args)
                {
                    // Check if the file exists
                    if (File.Exists(file))
                    {
                        // Add the file to the droppedObjects collection
                        droppedObjects.Add(new DroppedObject { Id = idCounter++, Name = Path.GetFileName(file), FullPath = Path.GetFullPath(file) });
                    }
                    else
                    {
                        // Log the file path if it does not exist
                        Console.WriteLine($"File not found: {file}");
                    }
                }

                // Create separate collections for images, videos, and GIFs
                List<DroppedObject> imageFiles = new List<DroppedObject>();
                List<DroppedObject> videoFiles = new List<DroppedObject>();
                List<DroppedObject> gifFiles = new List<DroppedObject>();

                // Log file path for unsupported files
                string unsupportedLogFilePath = Path.Combine(directoryPath, "unsupported_files.log");

                // Process files
                await FileProcessor.ProcessFilesAsync(droppedObjects, unsupportedLogFilePath, imageFiles, videoFiles, gifFiles);

                // Display the separated collections
                Logger.DisplayFiles("Image Files", imageFiles);
                Logger.DisplayFiles("Video Files", videoFiles);
                Logger.DisplayFiles("GIF Files", gifFiles);

                // Process images to add watermark
                await ImageProcessor.ProcessImagesAsync(imageFiles, watermarkPath);

                // Process videos to add watermark
                await ImageProcessor.ProcessVideosAsync(videoFiles, watermarkPath, videosOutputDirectory);

                // Process GIFs to add watermark
                await ImageProcessor.ProcessGifsAsync(gifFiles, watermarkPath);
            }
            catch (Exception ex)
            {
                // Log the exception to a file
                await File.AppendAllTextAsync(logFilePath, $"{DateTime.Now}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                // Display an error message
                Console.WriteLine("An error occurred. Please check the log file for details.");
            }

            // Stop the stopwatch and display the elapsed time
            stopwatch.Stop();
            // Display the total execution time
            Console.WriteLine($"Total execution time: {stopwatch.Elapsed}");

            // Wait for input before exiting
            Console.WriteLine("Press enter to finish");
            // End the program and exit
            Console.ReadLine();
        }

        /// <summary>
        /// Handles the console exit event to terminate all ffmpeg processes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A ConsoleCancelEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method is used to terminate all ffmpeg processes when the console is closed.
        /// May or may not be functioning as intended.
        /// </remarks>
        private static void OnConsoleExit(object? sender, ConsoleCancelEventArgs e)
        {
            // Terminate all ffmpeg processes
            foreach (var process in Process.GetProcessesByName("ffmpeg"))
            {
                // Attempt to kill the ffmpeg process
                try
                {
                    // Attempt to kill the ffmpeg process
                    process.Kill();

                    // Wait for the process to exit to ensure it has been terminated
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    // Log the exception details to a file if the process termination fails
                    // This includes the process ID, the exception message, and the stack trace
                    File.AppendAllText(logFilePath, $"{DateTime.Now}: Failed to terminate process {process.Id}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                }
            }
        }
    }
}