namespace AddIdentityToContent
{
    /// <summary>
    /// Provides logging functionalities to display lists of dropped files with their details.
    /// </summary>
    /// <remarks>
    /// This class is used to display the list of dropped files with their ID, name, and full path in a formatted manner.
    /// </remarks>
    /// <example>
    /// <code>
    /// Logger.DisplayFiles("Image Files", imageFiles);
    /// Logger.DisplayFiles("Video Files", videoFiles);
    /// Logger.DisplayFiles("GIF Files", gifFiles);
    /// </code>
    /// </example>
    /// <seealso cref="DroppedObject"/>
    internal static class Logger
    {
        /// <summary>
        /// Displays the list of dropped files with their ID, name, and full path in a formatted manner.
        /// </summary>
        /// <remarks>
        /// This method is used to display the list of dropped files with their ID, name, and full path in a formatted manner.
        /// This code is called in the Program.cs file to display the list of dropped files with their details.
        /// </remarks>
        /// <param name="title">The title to display before the list of files.</param>
        /// <param name="files">The list of dropped files to display.</param>
        /// <example>
        /// <code>
        /// Logger.DisplayFiles("Image Files", imageFiles);
        /// Logger.DisplayFiles("Video Files", videoFiles);
        /// Logger.DisplayFiles("GIF Files", gifFiles);
        /// </code>
        /// </example>
        /// <seealso cref="DroppedObject"/>
        public static void DisplayFiles(string title, List<DroppedObject> files)
        {
            // Set the console text color to red for the title
            Console.ForegroundColor = ConsoleColor.Red;
            // Print the title of the list
            Console.WriteLine($"\n{title}:");

            // Iterate through each file and display its details
            foreach (var file in files)
            {
                // Set the console text color to yellow for the ID
                Console.ForegroundColor = ConsoleColor.Yellow;
                // Print the ID, Name, and FullPath of the file
                Console.Write($"Id: {file.Id}, ");

                // Set the console text color to white for the Name
                Console.ForegroundColor = ConsoleColor.White;
                // Print the Name of the file
                Console.Write($"Name: {file.Name}, ");

                // Set the console text color to cyan for the FullPath
                Console.ForegroundColor = ConsoleColor.Cyan;
                // Print the FullPath of the file
                Console.WriteLine($"FullPath: {file.FullPath}");

                // Reset to default color, white usually
                Console.ResetColor();
                // Print a separator line for better readability
                Console.WriteLine("--------------------------------------------------");
            }

            // Reset the console text color to default, white usually
            Console.ResetColor();
            // Print a separator line for better readability
            Console.WriteLine("--------------------------------------------------");

            // Print the name of the list and state it is complete
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{title} list complete.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("------");

            // Reset to default color, white usually
            Console.ResetColor();
            // Print a separator line for better readability
            Console.WriteLine("--------------------------------------------------");
        }
    }
}
