namespace Shelly
{
    public partial class AddScenePage : ContentPage
    {
        private readonly IReadOnlyList<PanoramaScene> scenes;
        private FileResult? selectedImage;
        private PanoramaScene? loadedScene;

        public AddScenePage()
            : this([])
        {
        }

        public AddScenePage(IReadOnlyList<PanoramaScene> scenes, PanoramaScene? scene = null)
        {
            InitializeComponent();

            this.scenes = scenes;

            if (scene is not null)
            {
                LoadScene(scene);
            }
        }

        public event EventHandler<PanoramaScene>? SceneCreated;

        public event EventHandler<PanoramaScene>? SceneUpdated;

        public event EventHandler<PanoramaScene>? SceneDeleted;

        private void OnNameCompleted(object? sender, EventArgs e)
        {
            TryLoadSceneByName();
        }

        private void OnNameUnfocused(object? sender, FocusEventArgs e)
        {
            TryLoadSceneByName();
        }

        private void TryLoadSceneByName()
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            PanoramaScene? scene = scenes.FirstOrDefault(scene =>
                string.Equals(scene.Name, name, StringComparison.OrdinalIgnoreCase));

            if (scene is not null)
            {
                LoadScene(scene);
            }
        }

        private void LoadScene(PanoramaScene scene)
        {
            loadedScene = scene;
            selectedImage = null;
            TitleLabel.Text = "Edit scene";
            NameEntry.Text = scene.Name;
            SelectedImageLabel.Text = Path.GetFileName(scene.ImagePath);
            DeleteButton.IsVisible = true;
        }

        private async void OnChooseImageClicked(object? sender, EventArgs e)
        {
            selectedImage = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a panorama image",
                FileTypes = FilePickerFileType.Images
            });

            if (selectedImage is null)
            {
                return;
            }

            SelectedImageLabel.Text = selectedImage.FileName;

            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                NameEntry.Text = Path.GetFileNameWithoutExtension(selectedImage.FileName);
                TryLoadSceneByName();
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlertAsync("Missing details", "Enter a scene name.", "OK");
                return;
            }

            if (loadedScene is null)
            {
                if (selectedImage is null)
                {
                    await DisplayAlertAsync("Missing details", "Choose a panorama image.", "OK");
                    return;
                }

                string savedImagePath = await SaveSceneImageAsync(selectedImage);
                var scene = new PanoramaScene
                {
                    Name = name,
                    ImagePath = savedImagePath
                };

                SceneCreated?.Invoke(this, scene);
                await Navigation.PopModalAsync();
                return;
            }

            string previousImagePath = loadedScene.ImagePath;
            loadedScene.Name = name;

            if (selectedImage is not null)
            {
                loadedScene.ImagePath = await SaveSceneImageAsync(selectedImage);
                DeleteSceneImageIfOwned(previousImagePath);
            }

            SceneUpdated?.Invoke(this, loadedScene);
            await Navigation.PopModalAsync();
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (loadedScene is null)
            {
                return;
            }

            bool shouldDelete = await DisplayAlertAsync(
                "Delete scene",
                "Delete this scene and all hotspots connected to it?",
                "Delete",
                "Cancel");

            if (!shouldDelete)
            {
                return;
            }

            SceneDeleted?.Invoke(this, loadedScene);
            await Navigation.PopModalAsync();
        }

        private static async Task<string> SaveSceneImageAsync(FileResult image)
        {
            string extension = Path.GetExtension(image.FileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpeg";
            }

            string scenesDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "Resources",
                "Images",
                "Scenes");

            Directory.CreateDirectory(scenesDirectory);

            string savedImagePath = Path.Combine(scenesDirectory, $"{Guid.NewGuid():N}{extension}");

            await using Stream sourceStream = await image.OpenReadAsync();
            await using FileStream destinationStream = File.Create(savedImagePath);
            await sourceStream.CopyToAsync(destinationStream);

            return savedImagePath;
        }

        public static void DeleteSceneImageIfOwned(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !Path.IsPathRooted(imagePath))
            {
                return;
            }

            string scenesDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "Resources",
                "Images",
                "Scenes");

            string fullImagePath = Path.GetFullPath(imagePath);
            string fullScenesDirectory = Path.GetFullPath(scenesDirectory);

            if (!fullImagePath.StartsWith(fullScenesDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(fullImagePath))
            {
                File.Delete(fullImagePath);
            }
        }
    }
}
