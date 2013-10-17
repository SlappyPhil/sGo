using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Kinect;

namespace Programming_For_Kinect_Book
{
    public class SkeletonDisplayManager
    {
        readonly Canvas rootCanvas;
        readonly KinectSensor sensor;

        public SkeletonDisplayManager(KinectSensor kinectSensor, Canvas root)
        {
            rootCanvas = root;
            sensor = kinectSensor;
        }

        void GetCoordinates(JointType jointType, IEnumerable<Joint> joints, out float x, out float y)
        {
            var joint = joints.First(j => j.JointType == jointType);

            Vector2 vector2 = Tools.Convert(sensor, joint.Position);

            x = (float)(vector2.X * rootCanvas.ActualWidth);
            y = (float)(vector2.Y * rootCanvas.ActualHeight);
        }

        void Plot(JointType centerID, IEnumerable<Joint> joints, Color color)
        {
            float centerX;
            float centerY;

            GetCoordinates(centerID, joints, out centerX, out centerY);

            const double diameter = 8;

            Ellipse ellipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 4.0,
                Stroke = new SolidColorBrush(color),
                StrokeLineJoin = PenLineJoin.Round
            };

            Canvas.SetLeft(ellipse, centerX - ellipse.Width / 2);
            Canvas.SetTop(ellipse, centerY - ellipse.Height / 2);

            rootCanvas.Children.Add(ellipse);
        }

        void Plot(JointType centerID, JointType baseID, JointCollection joints, Color color)
        {
            float centerX;
            float centerY;

            GetCoordinates(centerID, joints, out centerX, out centerY);

            float baseX;
            float baseY;

            GetCoordinates(baseID, joints, out baseX, out baseY);

            double diameter = Math.Abs(baseY - centerY);

            Ellipse ellipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 4.0,
                Stroke = new SolidColorBrush(color),
                StrokeLineJoin = PenLineJoin.Round
            };

            Canvas.SetLeft(ellipse, centerX - ellipse.Width / 2);
            Canvas.SetTop(ellipse, centerY - ellipse.Height / 2);

            rootCanvas.Children.Add(ellipse);
        }

        void Trace(JointType sourceID, JointType destinationID, JointCollection joints, Color color)
        {
            float sourceX;
            float sourceY;

            GetCoordinates(sourceID, joints, out sourceX, out sourceY);

            float destinationX;
            float destinationY;

            GetCoordinates(destinationID, joints, out destinationX, out destinationY);

            Line line = new Line
            {
                X1 = sourceX,
                Y1 = sourceY,
                X2 = destinationX,
                Y2 = destinationY,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeThickness = 4.0,
                Stroke = new SolidColorBrush(color),
                StrokeLineJoin = PenLineJoin.Round
            };


            rootCanvas.Children.Add(line);
        }

        //private void DrawBone(Joint jointFrom, Joint jointTo)
        //{
        //    if (jointFrom.TrackingState == JointTrackingState.NotTracked ||
        //    jointTo.TrackingState == JointTrackingState.NotTracked)
        //    {
        //        return; // nothing to draw, one of the joints is not tracked
        //    }

        //    if (jointFrom.TrackingState == JointTrackingState.Inferred ||
        //    jointTo.TrackingState == JointTrackingState.Inferred)
        //    {
        //        DrawNonTrackedBoneLine(jointFrom.Position, jointTo.Position);  // Draw thin lines if either one of the joints is inferred
        //    }

        //    if (jointFrom.TrackingState == JointTrackingState.Tracked &&
        //    jointTo.TrackingState == JointTrackingState.Tracked)
        //    {
        //        DrawTrackedBoneLine(jointFrom.Position, jointTo.Position);  // Draw bold lines if the joints are both tracked
        //    }
        //}

        private void Draw(Skeleton[] skeletons, Color color)
        {
            rootCanvas.Children.Clear();
            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;
                Plot(JointType.HandLeft, skeleton.Joints, color);
                Trace(JointType.HandLeft, JointType.WristLeft, skeleton.Joints, color);
                Plot(JointType.WristLeft, skeleton.Joints, color);
                Trace(JointType.WristLeft, JointType.ElbowLeft, skeleton.Joints, color);
                Plot(JointType.ElbowLeft, skeleton.Joints, color);
                Trace(JointType.ElbowLeft, JointType.ShoulderLeft, skeleton.Joints, color);
                Plot(JointType.ShoulderLeft, skeleton.Joints, color);
                Trace(JointType.ShoulderLeft, JointType.ShoulderCenter, skeleton.Joints, color);

                Plot(JointType.ShoulderCenter, skeleton.Joints, color);
                Trace(JointType.ShoulderCenter, JointType.Head, skeleton.Joints, color);
                Plot(JointType.Head, JointType.ShoulderCenter, skeleton.Joints, color);
                Trace(JointType.ShoulderCenter, JointType.ShoulderRight, skeleton.Joints, color);

                Plot(JointType.ShoulderRight, skeleton.Joints, color);
                Trace(JointType.ShoulderRight, JointType.ElbowRight, skeleton.Joints, color);
                Plot(JointType.ElbowRight, skeleton.Joints, color);
                Trace(JointType.ElbowRight, JointType.WristRight, skeleton.Joints, color);
                Plot(JointType.WristRight, skeleton.Joints, color);
                Trace(JointType.WristRight, JointType.HandRight, skeleton.Joints, color);
                Plot(JointType.HandRight, skeleton.Joints, color);

                Trace(JointType.ShoulderCenter, JointType.Spine, skeleton.Joints, color);
                Plot(JointType.Spine, skeleton.Joints, color);
                Trace(JointType.Spine, JointType.HipCenter, skeleton.Joints, color);
                Plot(JointType.HipCenter, skeleton.Joints, color);
                Trace(JointType.HipCenter, JointType.HipLeft, skeleton.Joints, color);
                Plot(JointType.HipLeft, skeleton.Joints, color);
                Trace(JointType.HipLeft, JointType.KneeLeft, skeleton.Joints, color);
                Plot(JointType.KneeLeft, skeleton.Joints, color);
                Trace(JointType.KneeLeft, JointType.AnkleLeft, skeleton.Joints, color);
                Plot(JointType.AnkleLeft, skeleton.Joints, color);
                Trace(JointType.AnkleLeft, JointType.FootLeft, skeleton.Joints, color);
                Plot(JointType.FootLeft, skeleton.Joints, color);

                Trace(JointType.HipCenter, JointType.HipRight, skeleton.Joints, color);
                Plot(JointType.HipRight, skeleton.Joints, color);
                Trace(JointType.HipRight, JointType.KneeRight, skeleton.Joints, color);
                Plot(JointType.KneeRight, skeleton.Joints, color);
                Trace(JointType.KneeRight, JointType.AnkleRight, skeleton.Joints, color);
                Plot(JointType.AnkleRight, skeleton.Joints, color);
                Trace(JointType.AnkleRight, JointType.FootRight, skeleton.Joints, color);
                Plot(JointType.FootRight, skeleton.Joints, color);
            }
        }

        public void DrawStable(Skeleton skeleton)
        {
            Color color = Colors.Green;
            Skeleton[] skeletons = new Skeleton[1];
            skeletons[0] = skeleton;
            this.Draw(skeletons, color);
        }

        public void DrawUnstable(Skeleton skeleton)
        {
            Color color = Colors.Red;
            Skeleton[] skeletons = new Skeleton[1];
            skeletons[0] = skeleton;
            this.Draw(skeletons, color);
        }

        public void drawPositionOnly(Skeleton skeleton)
        {
            Color color = Colors.Blue;
            Plot(JointType.Spine, skeleton.Joints, color);
        }

        public void EraseCanvas()
        {
            rootCanvas.Children.Clear();
        }
    }
}