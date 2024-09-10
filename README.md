![Watermark](watermark.png)

# AddIdentityToContent

AddIdentityToContent is a free and open-source software (FOSS) project designed to add unique identifiers to content. This can be useful for tracking, referencing, or managing content in various applications.

## Features

- **Customizable**: Configure the format and type of identifiers to suit your needs.
- **Open Source**: Contribute to the project or customize it for your own use.

## Installation

To install AddIdentityToContent, you can clone the repository and build the project using your preferred method.

```bash
git clone https://github.com/yourusername/AddIdentityToContent.git cd AddIdentityToContent
```

Follow the build instructions for your environment to compile the project.

## Prerequisites

AddIdentityToContent requires the following files to be downloaded and added to the folder where the executable AddIdentityToContent.exe is located:

- [ffprobe.exe](https://www.ffmpeg.org/download.html) - for video processing.
- [ffmpeg.exe](https://www.ffmpeg.org/download.html) - for video processing.
- [ffmpegplay.exe](https://www.ffmpeg.org/download.html) - for video processing.

## Usage

Here's a basic example of how to use AddIdentityToContent in your project:

1. Compile the project as a console app, then run the executable by dropping files pre-selected from virtually anywhere on your system, the converting begins after listing the files to be converted.

1. The program will then convert the files to the desired format, and save them in categorised folders in the same directory as the original files.

1. You will find gifs, images, and videos, as folder names, and the files will be saved in the respective folders.

1. Gifs, may or may not convert in one go and stick the watermark on, the aim with gifs is to convert them to video files first, then use the converted video files to add the watermark. This speeds up the process and ensures that the watermark is added to the gif files.

1. This also results in a reduced file size.

## About the Nugets

The project uses the following Nugets:

- [CommandLineParser 2.8.0](https://www.nuget.org/packages/CommandLineParser/2.8.0) - for parsing command line arguments.
- [ImageMagick 14.0.0](https://www.nuget.org/packages/Magick.NET-Q16-AnyCPU/) - for image processing.
- [FFmpeg](https://www.ffmpeg.org/download.html) - for video processing. [See Xabe.FFmpeg Below]
- [Xabe.FFmpeg 5.2.6](https://www.nuget.org/packages/Xabe.FFmpeg) - for video processing.

The following features are utilised:

- ImageMagick is used to add the watermark to images.

- MediaToolkit is used to convert gifs to videos.

- ffmpeg is used to add the watermark to videos.

The following hardware accelleration is used:

- Nvidia GPU - for video processing. NVenc is used to encode the videos depending on the GPU.

- Intel GPU - for video processing. QuickSync is used to encode the videos depending on the GPU.

- AMD GPU - for video processing. AMF is used to encode the videos depending on the GPU.

- CPU - for image processing and video processing when no GPU is available.

OpenCL is used to detect the GPU and use the appropriate hardware accelleration.

## Contributing

We welcome contributions from the community! To contribute:

1. Fork the repository.
2. Create a new branch for your feature or bugfix.
3. Commit your changes.
4. Push your branch and create a pull request.

Please make sure to follow the [code of conduct](CODE_OF_CONDUCT.md) and [contribution guidelines](CONTRIBUTING.md).

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.md) file for details.
Also note, this project is an evolving product and is subject to change.

Please also read into the licensing for the other projects in the NuGet additions to ensure you comply with those licenses as well.

Please also see the project pages for Xabe.FFmpeg and FFmpeg for their respective licenses.
Please also see ImageMagick for their respective licenses.

Links are in the NuGets section above.

## Contact

For questions or suggestions, please open an issue on GitHub or contact the project maintainers.

A note from MrValentine7777:

I did what I could to make sure everything was covered in this project, but I am not a lawyer, so please make sure you read the licenses and comply with them, and also, do make a PR if you see something that needs to be fixed.

We are but human.

---

Though it is mentioned in Program.cs, I will mention it here as well:

This project was created with the help of GitHub CoPilot X Chat.

---

Thank you for using _AddIdentityToContent!_ Hope it helps you manage your content more effectively.