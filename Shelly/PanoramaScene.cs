namespace Shelly;

public sealed class PanoramaScene
{
    private const float FullEquirectangularAspectRatio = 2f;
    private const float AspectRatioTolerance = 0.05f;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public int FullPanoramaWidth { get; set; }

    public int FullPanoramaHeight { get; set; }

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public int CroppedAreaLeftPixels { get; set; }

    public int CroppedAreaTopPixels { get; set; }

    public bool IsPartialPanorama =>
        ImageWidth != FullPanoramaWidth ||
        ImageHeight != FullPanoramaHeight;

    public PanoramaValidationResult Validate()
    {
        if (ImageWidth <= 0 || ImageHeight <= 0)
        {
            return new PanoramaValidationResult(
                false,
                false,
                false,
                "The panorama image dimensions must be greater than zero.");
        }

        bool isFullEquirectangular = IsApproximatelyFullEquirectangular(ImageWidth, ImageHeight);

        if (isFullEquirectangular)
        {
            return new PanoramaValidationResult(
                true,
                true,
                false,
                "This image is a full 2:1 equirectangular panorama.");
        }

        if (FullPanoramaWidth <= 0 || FullPanoramaHeight <= 0)
        {
            return new PanoramaValidationResult(
                false,
                false,
                true,
                "This image is not a full 2:1 equirectangular panorama. Enter full panorama and crop metadata.");
        }

        if (ImageWidth > FullPanoramaWidth || ImageHeight > FullPanoramaHeight)
        {
            return new PanoramaValidationResult(
                false,
                false,
                true,
                "The cropped image dimensions cannot be larger than the full panorama dimensions.");
        }

        if (CroppedAreaLeftPixels < 0 || CroppedAreaTopPixels < 0)
        {
            return new PanoramaValidationResult(
                false,
                false,
                true,
                "Crop offsets cannot be negative.");
        }

        if (CroppedAreaLeftPixels + ImageWidth > FullPanoramaWidth ||
            CroppedAreaTopPixels + ImageHeight > FullPanoramaHeight)
        {
            return new PanoramaValidationResult(
                false,
                false,
                true,
                "The cropped image must fit inside the full panorama dimensions.");
        }

        return new PanoramaValidationResult(
            true,
            false,
            true,
            "This image is not a full 2:1 equirectangular panorama. It will be loaded as a cropped panorama. Avoid stretching it to fit a 2:1 canvas.");
    }

    public static PanoramaScene CreatePartialPanorama(
        string name,
        string imagePath,
        int imageWidth,
        int imageHeight,
        int fullPanoramaWidth,
        int fullPanoramaHeight,
        int croppedAreaLeftPixels,
        int croppedAreaTopPixels)
    {
        return new PanoramaScene
        {
            Name = name,
            ImagePath = imagePath,
            ImageWidth = imageWidth,
            ImageHeight = imageHeight,
            FullPanoramaWidth = fullPanoramaWidth,
            FullPanoramaHeight = fullPanoramaHeight,
            CroppedAreaLeftPixels = croppedAreaLeftPixels,
            CroppedAreaTopPixels = croppedAreaTopPixels
        };
    }

    public static bool IsApproximatelyFullEquirectangular(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        float aspectRatio = (float)width / height;
        return Math.Abs(aspectRatio - FullEquirectangularAspectRatio) <= AspectRatioTolerance;
    }
}

public sealed record PanoramaValidationResult(
    bool IsValid,
    bool IsFullEquirectangular,
    bool IsPartialPanorama,
    string Message);
