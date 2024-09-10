namespace AddIdentityToContent
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ImageProcessor
    {
        /// <summary>
        /// Processes the images asynchronously by adding a watermark to each image.
        /// </summary>
        /// <param name="imageFiles"></param>
        /// <param name="watermarkPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task ProcessImagesAsync(List<DroppedObject> imageFiles, string watermarkPath)
        {
            // Create the output directory for the processed images
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "images");
            // Create the output directory if it does not exist
            Directory.CreateDirectory(outputDirectory);

            // Enable OpenCL
            OpenCL.IsEnabled = true;
            // Inform the user if OpenCL is enabled or disabled
            // This can be useful for debugging and troubleshooting
            // If OpenCL is disabled, some operations may fall back to CPU processing
            // If OpenCL is enabled, the GPU will be used for processing
            // This can significantly speed up image processing operations
            // Note: OpenCL must be supported by the GPU and the drivers for it to work
            // This uses a feature of the ImageMagick library to enable OpenCL support
            // A ?: ternary operator is used to display a message based on the IsEnabled property
            Console.WriteLine($"OpenCL is {(OpenCL.IsEnabled ? "enabled" : "disabled")}.");

            // Inform the user which GPU acceleration is enabled
            Console.WriteLine($"Available hardware acceleration codec: {GetHardwareAccelerationCodec()}");

            // Process each image file in the list
            Console.WriteLine("------------------");

            // Process each image file in the list
            using (var watermark = new MagickImage(watermarkPath))
            {
                // Limit the number of concurrent tasks to 8
                var tasks = imageFiles.Select(async (imageFile, index) =>
                {
                    // Check if the image file's path or name is null and throw an exception if so
                    if (imageFile?.FullPath == null || imageFile?.Name == null)
                        throw new ArgumentNullException(nameof(imageFile), "Image file path or name is null");

                    // Load the image file
                    using (var image = new MagickImage(imageFile.FullPath))
                    {
                        // Calculate the maximum dimensions for the watermark
                        int maxWidth = (int)(image.Width / 3);
                        int maxHeight = (int)(image.Height / 3);

                        // Resize watermark if it exceeds the 9-grid dimensions
                        if (watermark.Width > maxWidth || watermark.Height > maxHeight)
                        {
                            watermark.Resize((uint)maxWidth, (uint)maxHeight);
                        }

                        // Composite the watermark onto the image
                        image.Composite(watermark, Gravity.Southwest, CompositeOperator.Over);

                        // Save the image with the watermark
                        string outputFilePath = Path.Combine(outputDirectory, Path.GetFileName(imageFile.FullPath));
                        // Write the image to the output directory
                        await image.WriteAsync(outputFilePath);
                    }

                    // Calculate and display progress percentage
                    double progressPercentage = ((double)(index + 1) / imageFiles.Count) * 100;
                    DisplayProgressBar(index + 1, imageFiles.Count, imageFile.Name);
                    // Display progress information
                    Console.WriteLine($"Processed {imageFile.Name} ({index + 1}/{imageFiles.Count}) - Progress: {progressPercentage:F2}% ");
                });

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Processes the GIF files asynchronously by converting them to MP4 and adding a watermark.
        /// </summary>
        /// <param name="gifFiles"></param>
        /// <param name="watermarkPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task ProcessGifsAsync(List<DroppedObject> gifFiles, string watermarkPath)
        {
            // Create output directories for GIFs and videos
            string gifsOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "gifs");
            string videosOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "videos");
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "error.log");
            Directory.CreateDirectory(gifsOutputDirectory);
            Directory.CreateDirectory(videosOutputDirectory);

            // foreground colour green
            Console.ForegroundColor = ConsoleColor.Green;

            // Enable OpenCL for ImageMagick
            OpenCL.IsEnabled = true;
            // Inform the user if OpenCL is enabled or disabled
            // Using a ternary operator to display a message based on the IsEnabled property
            Console.WriteLine($"OpenCL is {(OpenCL.IsEnabled ? "enabled" : "disabled")}.");

            // reset foreground colour
            Console.ResetColor();

            // Collection to store MP4 files for further processing
            var videoFiles = new List<DroppedObject>();

            // Limit the number of concurrent tasks to 8
            using (var semaphore = new SemaphoreSlim(8))
            {
                // Create a collection of tasks to process each GIF file
                var tasks = gifFiles.Select(async (gifFile, index) =>
                {
                    // Wait for the semaphore to allow processing
                    await semaphore.WaitAsync();
                    // Wrap the processing in a try-finally block to ensure the semaphore is released
                    try
                    {
                        // Check if the GIF file's path or name is null and throw an exception if so
                        if (gifFile?.FullPath == null || gifFile?.Name == null)
                            throw new ArgumentNullException(nameof(gifFile), "GIF file path or name is null");

                        // Convert the GIF to MP4 with hardware acceleration
                        string mp4OutputFilePath = Path.Combine(gifsOutputDirectory, Path.ChangeExtension(Path.GetFileName(gifFile.FullPath), ".mp4"));
                        // Convert the GIF to MP4 using FFmpeg with hardware acceleration
                        await ConvertGifToMp4Async(gifFile.FullPath, mp4OutputFilePath);

                        // Add the converted MP4 to the collection for further processing
                        videoFiles.Add(new DroppedObject { FullPath = mp4OutputFilePath, Name = Path.GetFileName(mp4OutputFilePath) });

                        // Calculate and display progress percentage
                        double progressPercentage = ((double)(index + 1) / gifFiles.Count) * 100;
                        // Display progress information
                        Console.WriteLine($"Processed {gifFile.Name} ({index + 1}/{gifFiles.Count}) - Progress: {progressPercentage:F2}% ");
                    }
                    catch (Exception ex)
                    {
                        // Log the exception to a file
                        await File.AppendAllTextAsync(logFilePath, $"{DateTime.Now}: Error processing {gifFile?.Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                        // Inform the user about the error
                        Console.WriteLine($"Error processing {gifFile?.Name}. Please check the log file for details.");
                    }
                    finally
                    {
                        // Release the semaphore to allow other tasks to proceed
                        semaphore.Release();
                    }
                });

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
            }

            // Process the MP4 files to add the watermark and save to the videos folder
            await ProcessVideosAsync(videoFiles, watermarkPath, videosOutputDirectory);
        }

        /// <summary>
        /// Converts a GIF file to MP4 using FFmpeg with hardware acceleration.
        /// </summary>
        /// <param name="gifFilePath"></param>
        /// <param name="mp4FilePath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static Task ConvertGifToMp4Async(string gifFilePath, string mp4FilePath)
        {
            // Inform the user the conversion is starting
            Console.WriteLine("Starting conversion of GIF to MP4...");
            Console.WriteLine("This can take a while, depending on the size of the GIF and the number of frames.");
            Console.WriteLine("The progress bar will show the progress of the conversion, but it might not be very accurate.");
            Console.WriteLine("Please be patient, the conversion will finish, even if the progress bar is not at 100%.");

            // Start the FFmpeg process to convert the GIF to MP4
            return Task.Run(() =>
            {
                // set foreground colour to green
                Console.ForegroundColor = ConsoleColor.Green;

                // Detect the available GPU and set the appropriate codec
                string codec = GetHardwareAccelerationCodec();
                // Inform the user about the hardware acceleration codec being used
                Console.WriteLine($"Using hardware acceleration codec: {codec}");

                // reset foreground colour
                Console.ResetColor();

                // Prepare the ffmpeg process arguments
                var startInfo = new ProcessStartInfo
                {
                    // Set the filename to ffmpeg
                    FileName = "ffmpeg",
                    // Set the arguments to convert the GIF to MP4 with hardware acceleration
                    Arguments = $"-y -i \"{gifFilePath}\" -q:v 0 -c:v {codec} -movflags faststart -pix_fmt yuv420p \"{mp4FilePath}\"",
                    // Redirect the standard output to read the output of the command
                    RedirectStandardOutput = true,
                    // Redirect the standard error to read the error output of the command
                    RedirectStandardError = true,
                    // Do not use the shell to execute the command
                    UseShellExecute = false,
                    // Do not create a window for the process
                    CreateNoWindow = true
                };

                // Start the ffmpeg process
                using (var process = new Process { StartInfo = startInfo })
                {
                    // Handle the output and error data received from the FFmpeg process
                    process.OutputDataReceived += (sender, args) => ParseMp4ConversionProgress(args?.Data, gifFilePath);
                    // Handle the error data received from the FFmpeg process
                    process.ErrorDataReceived += (sender, args) => ParseMp4ConversionProgress(args?.Data, gifFilePath);

                    // Start the FFmpeg process
                    process.Start();
                    // Begin reading the output and error streams asynchronously
                    process.BeginOutputReadLine();
                    // Begin reading the error stream asynchronously
                    process.BeginErrorReadLine();
                    // Wait for the FFmpeg process to exit
                    process.WaitForExit();

                    // Check the exit code of the FFmpeg process
                    if (process.ExitCode != 0)
                    {
                        // Throw an exception if the FFmpeg process fails
                        throw new InvalidOperationException($"FFmpeg process failed with exit code {process.ExitCode}");
                    }
                }
            });
        }

        /// <summary>
        /// Displays a progress bar for the image processing.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="total"></param>
        /// <param name="fileName"></param>
        private static void DisplayProgressBar(double progress, int total, string fileName)
        {
            // Define the width of the progress bar in characters
            int progressBarWidth = 50;
            int progressBlocks = (int)((progress / total) * progressBarWidth);

            // Move the cursor to the beginning of the line
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.Write(new string('#', progressBlocks));
            Console.Write(new string(' ', progressBarWidth - progressBlocks));
            Console.Write($"] \t_\t Item# {progress} - {fileName}");
            Console.CursorLeft = 0;
        }

        /// <summary>
        /// Processes the video files asynchronously by adding a watermark to each video.
        /// </summary>
        /// <param name="videoFiles">The list of video files to process.</param>
        /// <param name="watermarkPath">The path to the watermark image to be added to the videos.</param>
        /// <param name="outputDirectory">The directory where the processed videos will be saved.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a video file's path or name is null.</exception>
        public static async Task ProcessVideosAsync(List<DroppedObject> videoFiles, string watermarkPath, string outputDirectory)
        {
            // Create the output directory if it does not exist
            Directory.CreateDirectory(outputDirectory);

            // Limit the number of concurrent tasks to 8
            // This is to prevent overloading the system with too many FFmpeg processes
            using (var semaphore = new SemaphoreSlim(8))
            {
                // Create a collection of tasks to process each video file
                var tasks = videoFiles.Select(async (videoFile, index) =>
                {
                    // Wait for the semaphore to allow processing
                    await semaphore.WaitAsync();
                    // Wrap the processing in a try-finally block to ensure the semaphore is released
                    try
                    {
                        // Check if the video file's path or name is null and throw an exception if so
                        if (videoFile?.FullPath == null || videoFile?.Name == null)
                            throw new ArgumentNullException(nameof(videoFile), "Video file path or name is null");

                        // Construct the output file path
                        // This will be the same as the input file name, but in the output directory
                        string outputFilePath = Path.Combine(outputDirectory, Path.GetFileName(videoFile.FullPath));

                        // Add the watermark to the video and save it to the output directory
                        await AddWatermarkToVideoAsync(videoFile.FullPath, watermarkPath, outputFilePath, index + 1, videoFiles.Count);

                        // Display progress information
                        // This will show the progress of the current video file being processed
                        // The progress is displayed as a percentage and the current file's index
                        Console.WriteLine($"Processed {videoFile.Name} ({index + 1}/{videoFiles.Count})");
                    }
                    finally
                    {
                        // Release the semaphore to allow other tasks to proceed
                        semaphore.Release();
                    }
                });

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Adds a watermark to a video file asynchronously.
        /// </summary>
        /// <remarks>
        /// This method is used to add a watermark to a video file using FFmpeg with hardware acceleration.
        /// The watermark is added to the bottom-left corner of the video.
        /// The output video file is saved with the same name as the input file but with an MP4 extension.
        /// </remarks>
        /// <param name="inputFilePath">The path to the input video file.</param>
        /// <param name="watermarkPath">The path to the watermark image.</param>
        /// <param name="outputFilePath">The path to the output video file.</param>
        /// <param name="currentIndex">The current index of the video file being processed.</param>
        /// <param name="totalFiles">The total number of video files to be processed.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the FFmpeg process fails.</exception>
        private static Task AddWatermarkToVideoAsync(string inputFilePath, string watermarkPath, string outputFilePath, int currentIndex, int totalFiles)
        {
            // Start the FFmpeg process to add the watermark to the video
            return Task.Run(() =>
            {
                // Set the console text color to yellow
                Console.ForegroundColor = ConsoleColor.Yellow;

                // Detect the available GPU and set the appropriate codec
                string codec = GetHardwareAccelerationCodec();

                // Get the current process ID (parent process ID)
                int parentProcessId = Process.GetCurrentProcess().Id;

                // Inform the user about the hardware acceleration codec being used
                Console.WriteLine($"Running a new worker for file {currentIndex}/{totalFiles}");
                Console.WriteLine($"Using hardware acceleration codec: {codec}");

                // Reset the console color to default
                Console.ResetColor();

                // Get video dimensions
                var (videoWidth, videoHeight) = GetVideoDimensions(inputFilePath);

                // Calculate the maximum allowed dimensions for the watermark
                int maxWidth = videoWidth / 3;
                int maxHeight = videoHeight / 3;

                // Ensure the output file has an MP4 extension
                string mp4OutputFilePath = Path.ChangeExtension(outputFilePath, ".mp4");

                // Start the FFmpeg process with a watchdog script
                var startInfo = new ProcessStartInfo
                {
                    // Set the filename to ffmpeg
                    FileName = "ffmpeg",
                    // Set the arguments to add the watermark to the video with hardware acceleration
                    Arguments = $"-y -i \"{inputFilePath}\" -i \"{watermarkPath}\" -filter_complex \"[1:v]scale='if(gt(a,{maxWidth}/{maxHeight}),{maxWidth},-1)':'if(gt(a,{maxWidth}/{maxHeight}),-1,{maxHeight})'[wm];[0:v][wm]overlay=10:H-h-10:format=rgb\" -c:v {codec} -c:a aac -b:a 192k -movflags +faststart -pix_fmt yuv420p \"{mp4OutputFilePath}\"",
                    // Redirect the standard output to read the output of the command
                    RedirectStandardOutput = false,
                    // Redirect the standard error to read the error output of the command
                    RedirectStandardError = true,
                    // Do not use the shell to execute the command
                    UseShellExecute = false,
                    // Do not create a window for the process
                    CreateNoWindow = true
                };

                // Start the FFmpeg process
                using (var ffmpegProcess = new Process { StartInfo = startInfo })
                {
                    // Handle the error data received from the FFmpeg process
                    ffmpegProcess.ErrorDataReceived += (sender, args) =>
                    {
                        // Parse the progress information from the FFmpeg output
                        if (args?.Data != null)
                        {
                            ParseProgress(args.Data, currentIndex, totalFiles, inputFilePath);
                        }
                    };

                    // Start the FFmpeg process
                    ffmpegProcess.Start();
                    // Begin reading the error stream asynchronously
                    ffmpegProcess.BeginErrorReadLine();

                    // Watchdog task to check if the parent process is still running
                    Task.Run(async () =>
                    {
                        // Wait for the FFmpeg process to exit
                        while (!ffmpegProcess.HasExited)
                        {
                            // Check if the parent process is still running
                            if (!IsParentProcessRunning(parentProcessId))
                            {
                                // Kill the FFmpeg process if the parent process is no longer running
                                ffmpegProcess.Kill();
                                break;
                            }
                            // Wait for a short interval before checking again
                            await Task.Delay(1000); // Check every second
                        }
                    });

                    // Wait for the FFmpeg process to exit
                    ffmpegProcess.WaitForExit();

                    // Check the exit code of the FFmpeg process
                    if (ffmpegProcess.ExitCode != 0)
                    {
                        // Throw an exception if the FFmpeg process fails
                        throw new InvalidOperationException($"FFmpeg process failed with exit code {ffmpegProcess.ExitCode}");
                    }
                }
            });
        }

        /// <summary>
        /// Checks if the parent process is still running.
        /// </summary>
        /// <param name="parentProcessId">The ID of the parent process.</param>
        /// <returns>True if the parent process is still running; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when the process with the specified ID is not found.</exception>
        /// <remarks>
        /// This method is used to check if the parent process is still running.
        /// If the parent process is not running, the FFmpeg process is killed.
        /// </remarks>
        /// <seealso cref="Process.GetProcessById(int)"/>
        /// <seealso cref="Process"/>
        /// <seealso cref="ArgumentException"/>
        private static bool IsParentProcessRunning(int parentProcessId)
        {
            // Check if the parent process is still running
            try
            {
                // Attempt to get the process by its ID.
                // If the process is found, it means the parent process is still running.
                Process.GetProcessById(parentProcessId);
                return true;
            }
            catch (ArgumentException)
            {
                // If an ArgumentException is thrown, it means the process with the specified ID is not found.
                // This indicates that the parent process is no longer running.
                return false;
            }
        }

        /// <summary>
        /// Gets the dimensions (width and height) of a video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>A tuple containing the width and height of the video.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the video dimensions cannot be determined.</exception>
        /// <returns>The width and height of the video file.</returns>
        /// <remarks>
        /// This method is used to get the dimensions of a video file using FFprobe.
        /// The dimensions are returned as a tuple of width and height.
        /// </remarks>
        /// <seealso cref="ProcessStartInfo"/>
        /// <seealso cref="Process"/>
        private static (int Width, int Height) GetVideoDimensions(string filePath)
        {
            // Configure the process start information for ffprobe
            var ffprobe = new ProcessStartInfo
            {
                // Set the filename to ffprobe
                FileName = "ffprobe",
                // Set the arguments to get the video dimensions
                Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{filePath}\"",
                // Redirect the standard output to read the output of the command
                RedirectStandardOutput = true,
                // Do not use the shell to execute the command
                UseShellExecute = false,
                // Do not create a window for the process
                CreateNoWindow = true
            };

            // Start the ffprobe process to get the video dimensions
            using (var process = new Process { StartInfo = ffprobe })
            {
                // Start the process
                process.Start();
                // Read the output from the ffprobe process
                string output = process.StandardOutput.ReadToEnd();
                // Wait for the process to exit
                process.WaitForExit();

                // Split the output to get the width and height
                var dimensions = output.Trim().Split('x');
                // Check if the dimensions are valid
                if (dimensions.Length == 2 && int.TryParse(dimensions[0], out int width) && int.TryParse(dimensions[1], out int height))
                {
                    // Return the width and height as a tuple
                    return (width, height);
                }
                else
                {
                    // Throw an exception if the dimensions cannot be determined
                    throw new InvalidOperationException("Could not determine video dimensions.");
                }
            }
        }

        /// <summary>
        /// Parses the progress information from FFmpeg output and displays the progress bar.
        /// </summary>
        /// <remarks>
        /// This method is used to parse the progress information from the FFmpeg output.
        /// The progress information is displayed as a progress bar in the console.
        /// </remarks>
        /// <param name="data">The data output from the FFmpeg process.</param>
        /// <param name="currentIndex">The current index of the video file being processed.</param>
        /// <param name="totalFiles">The total number of video files to be processed.</param>
        /// <param name="inputFilePath">The path to the input video file.</param>
        /// <seealso cref="Regex"/>
        /// <seealso cref="TimeSpan"/>
        /// <seealso cref="GetVideoDuration(string)"/>
        /// <seealso cref="DisplayVideoProgressBar(double, int, int)"/>
        /// <seealso cref="Regex.Match(string, string)"/>
        /// <seealso cref="TimeSpan.Parse(string)"/>
        /// <seealso cref="TimeSpan.TotalSeconds"/>
        private static void ParseProgress(string data, int currentIndex, int totalFiles, string inputFilePath)
        {
            // Check if the data is null or empty
            if (string.IsNullOrEmpty(data))
                return;

            // Regex to parse the progress information from FFmpeg output
            var match = Regex.Match(data, @"time=(\d+:\d+:\d+.\d+)");
            // Check if the regex match is successful
            if (match.Success)
            {
                // Extract the time value from the matched group
                string time = match.Groups[1].Value;
                // Parse the extracted time value to a TimeSpan object
                TimeSpan currentTime = TimeSpan.Parse(time);
                // Get the total duration of the video
                TimeSpan totalTime = GetVideoDuration(inputFilePath);

                // Calculate the progress as a percentage
                double progress = currentTime.TotalSeconds / totalTime.TotalSeconds;
                // Display the video progress bar with the calculated progress
                DisplayVideoProgressBar(progress, currentIndex, totalFiles);
            }
        }

        /// <summary>
        /// Gets the duration of a video file.
        /// </summary>
        /// <param name="filePath">The path to the video file.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the duration of the video.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the video duration cannot be determined.</exception>
        /// <remarks>
        /// This method uses the FFprobe tool to extract the duration of the video file.
        /// </remarks>
        /// <seealso cref="ProcessStartInfo"/>
        /// <seealso cref="Process"/>
        /// <seealso cref="TimeSpan"/>
        private static TimeSpan GetVideoDuration(string filePath)
        {
            // Configure the process start information for ffprobe
            var ffprobe = new ProcessStartInfo
            {
                // Set the filename to ffprobe
                FileName = "ffprobe",
                // Set the arguments to get the video duration
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                // Redirect the standard output to read the output of the command
                RedirectStandardOutput = true,
                // Do not use the shell to execute the command
                UseShellExecute = false,
                // Do not create a window for the process
                CreateNoWindow = true
            };

            // Start the ffprobe process to get the video duration
            using (var process = new Process { StartInfo = ffprobe })
            {
                // Start the process
                process.Start();
                // Read the output from the ffprobe process
                string output = process.StandardOutput.ReadToEnd();
                // Wait for the process to exit
                process.WaitForExit();

                // Try to parse the output to a double representing the duration in seconds
                if (double.TryParse(output.Trim(), out double duration))
                {
                    // Return the duration as a TimeSpan object
                    return TimeSpan.FromSeconds(duration);
                }
                else
                {
                    // Throw an exception if the duration cannot be determined
                    throw new InvalidOperationException("Could not determine video duration.");
                }
            }
        }

        /// <summary>
        /// Displays a progress bar for video processing in the console.
        /// </summary>
        /// <param name="progress">The progress of the video processing as a value between 0 and 1.</param>
        /// <param name="currentFrame">The current frame being processed.</param>
        /// <param name="totalFrames">The total number of frames in the video.</param>
        /// <remarks>
        /// This method updates the console with a visual representation of the progress of video processing.
        /// The progress bar is displayed as a series of '#' characters, with the length proportional to the progress.
        /// </remarks>
        /// <seealso cref="Console"/>
        private static void DisplayVideoProgressBar(double progress, int currentFrame, int totalFrames)
        {
            // Define the width of the progress bar in characters
            int progressBarWidth = 50;
            // Calculate the number of blocks to display in the progress bar
            int progressBlocks = (int)(progress * progressBarWidth);

            // Move the cursor to the beginning of the line
            Console.CursorLeft = 0;
            // Write the opening bracket of the progress bar
            Console.Write("[");
            // Write the progress blocks
            Console.Write(new string('#', progressBlocks));
            // Write the remaining empty space in the progress bar
            Console.Write(new string(' ', progressBarWidth - progressBlocks));
            // Write the closing bracket and the progress percentage
            Console.Write($"] {progress:P0} - Frame {currentFrame}/{totalFrames}");
            // Move the cursor back to the beginning of the line to overwrite in the next update
            Console.CursorLeft = 0;
        }

        /// <summary>
        /// Gets the frame count of a GIF file.
        /// </summary>
        /// <param name="gifFilePath">The path to the GIF file.</param>
        /// <returns>The number of frames in the GIF file.</returns>
        /// <remarks>
        /// This method uses the Magick.NET library to load the GIF file and count the number of frames.
        /// </remarks>
        /// <seealso cref="MagickImageCollection"/>
        private static int GetGifFrameCount(string gifFilePath)
        {
            // Load the GIF file into a MagickImageCollection
            using (var collection = new MagickImageCollection(gifFilePath))
            {
                // Return the number of frames in the collection
                return collection.Count;
            }
        }

        /// <summary>
        /// Determines the appropriate hardware acceleration codec for video encoding.
        /// </summary>
        /// <returns>A string representing the codec to be used for hardware acceleration.</returns>
        /// <remarks>
        /// This method checks for the availability of NVIDIA, Intel, and AMD GPUs in the system.
        /// If a compatible GPU is found, it returns the corresponding hardware acceleration codec.
        /// If no compatible GPU is found, it defaults to software encoding using "libx264".
        /// </remarks>
        /// <seealso cref="IsNvidiaGpuAvailable"/>
        /// <seealso cref="IsIntelGpuAvailable"/>
        /// <seealso cref="IsAmdGpuAvailable"/>
        private static string GetHardwareAccelerationCodec()
        {
            // Check if an NVIDIA GPU is available
            if (IsNvidiaGpuAvailable())
            {
                // Return the NVIDIA hardware acceleration codec
                return "h264_nvenc";
            }

            // Check if an Intel GPU is available
            if (IsIntelGpuAvailable())
            {
                // Return the Intel hardware acceleration codec
                return "h264_qsv";
            }

            // Check if an AMD GPU is available
            if (IsAmdGpuAvailable())
            {
                // Return the AMD hardware acceleration codec
                return "h264_amf";
            }

            // Default to software encoding if no compatible GPU is found
            return "libx264";
        }

        /// <summary>
        /// Parses the progress information from FFmpeg output during MP4 conversion and displays the progress bar.
        /// </summary>
        /// <param name="data">The data output from the FFmpeg process.</param>
        /// <param name="gifFilePath">The path to the GIF file being converted.</param>
        /// <remarks>
        /// This method uses a regular expression to extract the current frame number from the FFmpeg output.
        /// It then calculates the progress as a percentage and displays it using a progress bar.
        /// </remarks>
        /// <seealso cref="Regex"/>
        /// <seealso cref="GetGifFrameCount(string)"/>
        /// <seealso cref="DisplayVideoProgressBar(double, int, int)"/>
        private static void ParseMp4ConversionProgress(string? data, string gifFilePath)
        {
            // Check if the data is null or empty
            if (string.IsNullOrEmpty(data))
                return;

            // Regex to parse the progress information from FFmpeg output
            var match = Regex.Match(data, @"frame=\s*(\d+)");
            // Check if the regex match is successful
            if (match.Success)
            {
                // Extract the current frame number from the matched group
                int frame = int.Parse(match.Groups[1].Value);
                // Get the total number of frames in the GIF file
                int totalFrames = GetGifFrameCount(gifFilePath);

                // Calculate the progress as a percentage
                double progress = (double)frame / totalFrames;
                // Display the video progress bar with the calculated progress
                DisplayVideoProgressBar(progress, frame, totalFrames);
            }
        }

        /// <summary>
        /// Checks if an NVIDIA GPU is available on the system.
        /// </summary>
        /// <returns><c>true</c> if an NVIDIA GPU is available; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method uses the "nvidia-smi" command to check for the presence of an NVIDIA GPU.
        /// It executes the command and checks the output to determine if an NVIDIA GPU is available.
        /// </remarks>
        /// <seealso cref="ProcessStartInfo"/>
        /// <seealso cref="Process"/>
        private static bool IsNvidiaGpuAvailable()
        {
            // Configure the process start information for nvidia-smi
            var startInfo = new ProcessStartInfo
            {
                // Set the filename to nvidia-smi
                // This is the command-line utility for NVIDIA GPUs
                // It provides information about the GPU and its usage
                FileName = "nvidia-smi",
                // Pass the "-L" argument to list the available GPUs
                Arguments = "-L",
                // Redirect the standard output to read the output of the command
                RedirectStandardOutput = true,
                // Do not use the shell to execute the command
                UseShellExecute = false,
                // Do not create a window for the process
                CreateNoWindow = true
            };

            // Start the nvidia-smi process to check for NVIDIA GPU
            using (var process = new Process { StartInfo = startInfo })
            {
                // Start the process
                process.Start();
                // Read the output from the nvidia-smi process
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                // Return true if the output is not empty, indicating an NVIDIA GPU is available
                return !string.IsNullOrEmpty(output);
            }
        }

        /// <summary>
        /// Checks if an Intel GPU is available on the system.
        /// </summary>
        /// <returns><c>true</c> if an Intel GPU is available; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method uses the "ffmpeg -hwaccels" command to check for the presence of an Intel GPU.
        /// It executes the command and checks the output for the "qsv" hardware acceleration capability.
        /// </remarks>
        /// <seealso cref="ProcessStartInfo"/>
        /// <seealso cref="Process"/>
        private static bool IsIntelGpuAvailable()
        {
            // Configure the process start information for ffmpeg
            var startInfo = new ProcessStartInfo
            {
                // Set the filename to ffmpeg
                // This is the command-line utility for video processing
                // It supports hardware acceleration using Intel Quick Sync Video (QSV)
                FileName = "ffmpeg",
                // Pass the "-hwaccels" argument to list the available hardware acceleration codecs
                Arguments = "-hwaccels",
                // Redirect the standard output to read the output of the command
                RedirectStandardOutput = true,
                // Do not use the shell to execute the command
                UseShellExecute = false,
                // Do not create a window for the process
                CreateNoWindow = true
            };

            // Start the ffmpeg process to check for Intel GPU
            using (var process = new Process { StartInfo = startInfo })
            {
                // Start the process
                process.Start();
                // Read the output from the ffmpeg process
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                // Return true if the output contains "qsv", indicating an Intel GPU is available
                return output.Contains("qsv");
            }
        }

        /// <summary>
        /// Checks if an AMD GPU is available on the system.
        /// </summary>
        /// <returns><c>true</c> if an AMD GPU is available; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method uses the "ffmpeg -hwaccels" command to check for the presence of an AMD GPU.
        /// It executes the command and checks the output for the "amf" hardware acceleration capability.
        /// </remarks>
        /// <seealso cref="ProcessStartInfo"/>
        /// <seealso cref="Process"/>
        private static bool IsAmdGpuAvailable()
        {
            // Configure the process start information for ffmpeg
            var startInfo = new ProcessStartInfo
            {
                // Set the filename to ffmpeg
                // This is the command-line utility for video processing
                // It supports hardware acceleration using AMD Advanced Media Framework (AMF)
                FileName = "ffmpeg",
                // Pass the "-hwaccels" argument to list the available hardware acceleration codecs
                Arguments = "-hwaccels",
                // Redirect the standard output to read the output of the command
                RedirectStandardOutput = true,
                // Do not use the shell to execute the command
                UseShellExecute = false,
                // Do not create a window for the process
                CreateNoWindow = true
            };

            // Start the ffmpeg process to check for AMD GPU
            using (var process = new Process { StartInfo = startInfo })
            {
                // Start the process
                process.Start();
                // Read the output from the ffmpeg process
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                // Return true if the output contains "amf", indicating an AMD GPU is available
                return output.Contains("amf");
            }
        }
    }
}
