using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Face
{
    public class MainViewModel
    {
        Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;

        public MainViewModel()
        {
            this.MyModel = new PlotModel { Title = "Emotions" };
            Draw();
        }


        private void Draw()
        {
            var faceImageDir = installedLocation.Path + @"\FramesWithFace\New folder\";
            var saveFile = installedLocation.Path + @"\FramesWithFace\New folder\saveFaces.txt";
            var frames = new List<FFrame>();
            var i = 0;

            string saveString;

            using (Stream f = File.OpenRead(saveFile))
            {
                using (var sr = new StreamReader(f))
                {
                    saveString = sr.ReadToEnd();
                }
            }
            var lines = new List<LineSeries>();

            var sc = new Scores();
            foreach (var s in sc.ToRankedList())
            {
                lines.Add(new LineSeries()
                {
                    StrokeThickness = 2,
                    MarkerSize = 3,
                    MarkerType = MarkerType.Circle,
                    CanTrackerInterpolatePoints = false,
                    Title = string.Format(s.Key),
                    Smooth = false,
                });
            }

            var faces = JsonConvert.DeserializeObject<FFrame[]>(saveString);
            foreach (var face in faces.OrderBy(x => PathToSecond(x.FileName)))
            {
                var seconds = PathToSecond(face.FileName);
                if (face.Emotions != null && face.Emotions.Any())
                {
                    foreach (var emo in face.Emotions.First().Scores.ToRankedList())
                    {
                        var line = lines.First(l => l.Title == emo.Key);
                        line.Points.Add(new DataPoint(seconds, emo.Value));
                    }
                }
            }
            lines.ForEach(l => MyModel.Series.Add(l));
        }

        private double PathToSecond(string path)
        {
            var name = path.Split('\\').Last().Replace(".png", "");
            return double.Parse(name);
            
        }

        public PlotModel MyModel { get; private set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        FaceHandler faceHandler = new FaceHandler();
        Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
        private readonly string personGroupId = "stvtestgroup";
        private readonly Guid personId = Guid.Parse("997870fb-4c84-4439-a837-de2bf5e9d5c3");
        FaceServiceClient faceServiceClient = new FaceServiceClient("bf0707c3c7dd44e2bf2504017f8c7c4f");

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await Init();
            await CreateFaces();
            await Training();
            await FindAllFaces();
        }

        async Task Init()
        {
            int count = 0;
            
            var folder = await installedLocation.GetFolderAsync("FramesWithFace");
            var listCopy = new List<Task<StorageFile>>();
            foreach (string imagePath in Directory.GetFiles(installedLocation.Path + @"\frames\"))
            {
                var faces = await faceHandler.Handle(imagePath);
                if (faces != null)
                {
                    if (faces.Any())
                    {
                        StorageFile photoFile = await StorageFile.GetFileFromPathAsync(imagePath);
                        listCopy.Add(photoFile.CopyAsync(folder).AsTask());
                        count++;
                    }
                }
            }
            await Task.WhenAll(listCopy.ToArray());
        }

        async Task CreateFaces()
        {
            var person = await faceServiceClient.GetPersonAsync(personGroupId, personId);

            foreach (var item in person.PersistedFaceIds)
            {
                await faceServiceClient.DeletePersonFaceAsync(personGroupId, personId, item);
                await Task.Delay(3000);
            }

            var faceImageDir = installedLocation.Path + @"\FramesWithFace\New folder\";

            foreach (string imagePath in Directory.GetFiles(faceImageDir))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    var face = (await faceHandler.Handle(imagePath)).First();
                    var rect = new FaceRectangle()
                    {
                        Left = (int)face.FaceBox.X,
                        Top = (int)face.FaceBox.Y,
                        Height = (int)face.FaceBox.Height,
                        Width = (int)face.FaceBox.Width
                    };
                    try
                    {
                        await faceServiceClient.AddPersonFaceAsync(
                             "stvtestgroup", Guid.Parse("997870fb-4c84-4439-a837-de2bf5e9d5c3"), s, targetFace: rect);
                        await Task.Delay(3000);
                    }
                    catch
                    {

                    }
                }
            }
        }

        async Task Training()
        {
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);

            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != Status.Running)
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }

        async Task FindAllFaces()
        {
            var faceImageDir = installedLocation.Path + @"\FramesWithFace\";
            var saveFile = installedLocation.Path + @"\FramesWithFace\New folder\saveFaces.txt";
            var frames = new List<FFrame>();
            foreach (string imagePath in Directory.GetFiles(faceImageDir))
            {
                try
                {
                    frames.Add(await FindFace(imagePath));
                    await Task.Delay(6000);
                }
                catch (Exception ex)
                {
                }
            }
            var saveString = JsonConvert.SerializeObject(frames);




            StorageFile photoFile = await StorageFile.GetFileFromPathAsync(saveFile);
            using (Stream f = await photoFile.OpenStreamForWriteAsync())
            {
                using (var sw = new StreamWriter(f))
                {
                    sw.Write(saveString);
                }
            }
        }

        async Task<FFrame> FindFace(string testImageFile)
        {
            var result = new FFrame();
            result.FileName = testImageFile;

            using (Stream s = File.OpenRead(testImageFile))
            {
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Debug.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Debug.WriteLine("No one identified");
                    }
                    else
                    {
                        var candidate = identifyResult.Candidates.Where(x => x.PersonId == personId).OrderByDescending(x => x.Confidence).FirstOrDefault();
                        var face = faces.First(x => x.FaceId == identifyResult.FaceId);

                        var rect = new Rectangle()
                        {
                            Top = face.FaceRectangle.Top,
                            Left = face.FaceRectangle.Left,
                            Width = face.FaceRectangle.Width,
                            Height = face.FaceRectangle.Height,
                        };

                        var emo = await Emotion(testImageFile, rect);
                        result.FaceFrames = new FaceFrame()
                        {
                            Result = identifyResult,
                            Face = face,
                        };
                        result.Emotions = emo;
                    }
                }
            }
            return result;
        }

        async Task<Emotion[]> Emotion(string imageFilePath, Rectangle rect)
        {
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient("151563bcf698473d81b03777e8d75f95");

            Debug.WriteLine("Calling EmotionServiceClient.RecognizeAsync()...");
            try
            {
                Emotion[] emotionResult;
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream, new[] { rect });
                    return emotionResult;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());
                return null;
            }

        }
    }
}
