namespace AddIdentityToContent
{
    /// <summary>
    /// Represents an object that has been dropped, containing its ID, name, and full path.
    /// </summary>
    /// <remarks>
    /// The ID is used to identify the dropped object.
    /// The name is the name of the dropped object.
    /// The full path is the full path of the dropped object.
    /// </remarks>
    /// <example>
    /// {
    ///  "Id": 1,
    ///  "Name": "file.txt",
    ///  "FullPath": "C:\\Users\\user\\Documents\\file.txt"
    /// }
    /// </example>
    internal class DroppedObject
    {
        /// <summary>
        /// Gets or sets the ID of the dropped object.
        /// </summary>
        /// <example>
        /// 1
        /// </example>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the dropped object.
        /// </summary>
        /// <example>
        /// file.txt
        /// </example>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the full path of the dropped object.
        /// </summary>
        /// <example>
        /// C:\Users\user\Documents\file.txt
        /// </example>
        public string? FullPath { get; set; }
    }
}
