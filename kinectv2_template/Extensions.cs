using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace kinectv2_template
{
    // got a hint Extension from here http://pterneas.com/wp-content/uploads/2014/03/KinectHandTracking.zip
    public static class Extensions
    {
        #region Camera
        // return Writable Bitmap for RGB Color frame from Kinect
        public static ImageSource ToBitmap(this ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        #endregion Camera

        #region Tracking Drawing
        
        public static void DrawSkeleton(this Canvas canvas, Body body, CoordinateMapper mapper, float ratio)
        {
            if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(joint, mapper, ratio);
            }

            canvas.DrawLine(body.Joints[JointType.Head], body.Joints[JointType.Neck], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft], mapper, ratio);
            canvas.DrawLine(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight], mapper, ratio);
        }

        public static void DrawPoint(this Canvas canvas, Joint joint, CoordinateMapper mapper, float ratio)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            ColorSpacePoint colorPoint = mapper.MapCameraPointToColorSpace(joint.Position);

            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.LightBlue)
            };

            // avoid exception on bad tracking
            if (float.IsInfinity(colorPoint.X) || float.IsInfinity(colorPoint.Y) || colorPoint.Y / ratio> canvas.Height) return;

            // allign ellipse on canvas (divide by ratio since for mapping)
            Canvas.SetLeft(ellipse, colorPoint.X / ratio);
            Canvas.SetTop(ellipse, colorPoint.Y / ratio);

            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second, CoordinateMapper mapper, float ratio)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            ColorSpacePoint firstPoint = mapper.MapCameraPointToColorSpace(first.Position);
            ColorSpacePoint secondPoint = mapper.MapCameraPointToColorSpace(second.Position);

            if (float.IsInfinity(firstPoint.X) || float.IsInfinity(firstPoint.Y) || firstPoint.Y/ratio > canvas.Height) return;
            if (float.IsInfinity(secondPoint.X) || float.IsInfinity(secondPoint.Y) || secondPoint.Y/ratio > canvas.Height) return;


            Line line = new Line
            {
                X1 = firstPoint.X/ratio,
                Y1 = firstPoint.Y / ratio,
                X2 = secondPoint.X / ratio,
                Y2 = secondPoint.Y / ratio,
                StrokeThickness = 2,
                Stroke = new SolidColorBrush(Colors.Yellow)
            };

            canvas.Children.Add(line);
        }

        public static void DrawFace(this Canvas canvas, int faceIndex, FaceFrameResult faceResult, float ratio)
        {
            var faceBoxSource = faceResult.FaceBoundingBoxInColorSpace;
            Rectangle faceEl = new Rectangle();
            faceEl.Stroke = Brushes.Cyan;
            faceEl.StrokeThickness = 2;
            faceEl.Width = (faceBoxSource.Right - faceBoxSource.Left) / ratio;
            faceEl.Height = (faceBoxSource.Bottom - faceBoxSource.Top) / ratio;

            canvas.Children.Add(faceEl);

            Canvas.SetLeft(faceEl, faceBoxSource.Left / ratio);
            Canvas.SetTop(faceEl, faceBoxSource.Top / ratio);

            if (faceResult.FacePointsInColorSpace != null)
            {
                // draw each face point
                foreach (PointF pointF in faceResult.FacePointsInColorSpace.Values)
                {
                    Ellipse el = new Ellipse();
                    el.Width = el.Height = 10;
                    el.Fill = Brushes.Red;
                    el.StrokeThickness = 1;
                    canvas.Children.Add(el);
                    if (float.IsInfinity(pointF.X) || float.IsInfinity(pointF.Y)) return;
                    Canvas.SetLeft(el, pointF.X / ratio);
                    Canvas.SetTop(el, pointF.Y / ratio);
                }
            }
        }

        #endregion Tracking Drawing
    }
}
