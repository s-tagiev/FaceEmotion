using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Face
{
    class FaceHandler
    {

        private readonly uint sourceImageHeightLimit = 1280;

        /// <summary>
        /// Loads an image file (selected by the user) and runs the FaceDetector on the loaded bitmap. If successful calls SetupVisualization to display the results.
        /// </summary>
        /// <param name="sender">Button user clicked</param>
        /// <param name="e">Event data</param>
        public async Task<IList<DetectedFace>> Handle(string path)
        {
            IList<DetectedFace> faces = null;
            SoftwareBitmap detectorInput = null;
            WriteableBitmap displaySource = null;

            try
            {

                StorageFile photoFile = await StorageFile.GetFileFromPathAsync(path);
                if (photoFile == null)
                {
                    return null;
                }

                //this.ClearVisualization();
                //this.rootPage.NotifyUser("Opening...", NotifyType.StatusMessage);

                // Open the image file and decode the bitmap into memory.
                // We'll need to make 2 bitmap copies: one for the FaceDetector and another to display.
                using (IRandomAccessStream fileStream = await photoFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    BitmapTransform transform = this.ComputeScalingTransformForSourceImage(decoder);

                    using (SoftwareBitmap originalBitmap = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat, BitmapAlphaMode.Ignore, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage))
                    {
                        // We need to convert the image into a format that's compatible with FaceDetector.
                        // Gray8 should be a good type but verify it against FaceDetector’s supported formats.
                        const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Gray8;
                        if (FaceDetector.IsBitmapPixelFormatSupported(InputPixelFormat))
                        {
                            using (detectorInput = SoftwareBitmap.Convert(originalBitmap, InputPixelFormat))
                            {
                                // Create a WritableBitmap for our visualization display; copy the original bitmap pixels to wb's buffer.
                                displaySource = new WriteableBitmap(originalBitmap.PixelWidth, originalBitmap.PixelHeight);
                                originalBitmap.CopyToBuffer(displaySource.PixelBuffer);

                                //this.rootPage.NotifyUser("Detecting...", NotifyType.StatusMessage);

                                // Initialize our FaceDetector and execute it against our input image.
                                // NOTE: FaceDetector initialization can take a long time, and in most cases
                                // you should create a member variable and reuse the object.
                                // However, for simplicity in this scenario we instantiate a new instance each time.
                                FaceDetector detector = await FaceDetector.CreateAsync();
                                return await detector.DetectFacesAsync(detectorInput);

                                // Create our display using the available image and face results.
                                
                            }
                        }
                        else
                        {
                            //this.rootPage.NotifyUser("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector", NotifyType.ErrorMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //this.ClearVisualization();
               // this.rootPage.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
            }
            return null;
        }

        /// <summary>
        /// Updates any existing face bounding boxes in response to changes in the size of the Canvas.
        /// </summary>
        /// <param name="sender">Canvas object whose size has changed</param>
        /// <param name="e">Event data</param>
        //private void PhotoCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    try
        //    {
        //        // If the Canvas is resized we must recompute a new scaling factor and
        //        // apply it to each face box.
        //        if (this.PhotoCanvas.Background != null)
        //        {
        //            WriteableBitmap displaySource = (this.PhotoCanvas.Background as ImageBrush).ImageSource as WriteableBitmap;

        //            double widthScale = displaySource.PixelWidth / this.PhotoCanvas.ActualWidth;
        //            double heightScale = displaySource.PixelHeight / this.PhotoCanvas.ActualHeight;

        //            foreach (var item in PhotoCanvas.Children)
        //            {
        //                Rectangle box = item as Rectangle;
        //                if (box == null)
        //                {
        //                    continue;
        //                }

        //                // We saved the original size of the face box in the rectangles Tag field.
        //                BitmapBounds faceBounds = (BitmapBounds)box.Tag;
        //                box.Width = (uint)(faceBounds.Width / widthScale);
        //                box.Height = (uint)(faceBounds.Height / heightScale);

        //                box.Margin = new Thickness((uint)(faceBounds.X / widthScale), (uint)(faceBounds.Y / heightScale), 0, 0);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        this.rootPage.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
        //    }
        //}

        private BitmapTransform ComputeScalingTransformForSourceImage(BitmapDecoder sourceDecoder)
        {
            BitmapTransform transform = new BitmapTransform();

            if (sourceDecoder.PixelHeight > this.sourceImageHeightLimit)
            {
                float scalingFactor = (float)this.sourceImageHeightLimit / (float)sourceDecoder.PixelHeight;

                transform.ScaledWidth = (uint)Math.Floor(sourceDecoder.PixelWidth * scalingFactor);
                transform.ScaledHeight = (uint)Math.Floor(sourceDecoder.PixelHeight * scalingFactor);
            }

            return transform;
        }
    }
}
