using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;
using Emgu.CV.Structure;
using GLab.Core.Forms;

namespace Frame.VrAibo
{
    static class ImageOperations
    {
        public static void filterViaHSV(Image<Hsv, byte> img, Hsv colorToFilter, out Image<Gray, byte> mask)
        {
            Hsv hsv_min = new Hsv(colorToFilter.Hue, 0, 0);
            Hsv hsv_max = new Hsv(colorToFilter.Hue, 255, 255);
            mask = img.InRange(hsv_min, hsv_max);
        }

        public static void getMask(Image<Hsv, byte> img, Hsv colorToFilter, out Image<Gray, byte> mask, int hueRange, int strucSize)
        {
            mask = img.InRange(new Hsv(colorToFilter.Hue - hueRange / 2, 0, 0), new Hsv(colorToFilter.Hue + hueRange / 2, 255, 255));

            StructuringElementEx se = new StructuringElementEx(10, 10, 5, 5, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT);

            CvInvoke.cvErode(mask, mask, se, strucSize);
            CvInvoke.cvDilate(mask, mask, se, strucSize);

        }

        public static void getMask(Image<Rgb, byte> img, Hsv colorToFilter, out Image<Gray, byte> mask, int hueRange, int strucSize)
        {
            Image<Hsv, byte> hsvFront = new Image<Hsv, byte>(img.Size);

            CvInvoke.cvCvtColor(img, hsvFront, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);

            getMask(hsvFront, colorToFilter, out mask, hueRange, strucSize);

        }

        public static void maskMultipleColors(Image<Rgb, byte> src, out Image<Gray, byte> dest, List<Hsv> colorsToMask, int hueRange, int strucSize)
        {
            Image<Hsv, byte> hsvFront = new Image<Hsv, byte>(src.Size);
            CvInvoke.cvCvtColor(src, hsvFront, Emgu.CV.CvEnum.COLOR_CONVERSION.RGB2HSV);

            if (colorsToMask.Count > 0)
            {
                getMask(src, colorsToMask[0], out dest, hueRange, strucSize);
            }
            else
            {
                //if the list is empty
                dest = new Image<Gray, byte>(src.Width, src.Height, new Gray(0));

            }

            for (int i = 1; i < colorsToMask.Count; i++)
            {
                Image<Gray, byte> mask2;
                ImageOperations.getMask(src, colorsToMask[i], out mask2, hueRange, strucSize);

                //add the two mask together
                dest = dest.AddWeighted(mask2, 1, 1, 0);
            }
        }

        public static int scanHorizontal(Image<Rgb, byte> img, System.Drawing.Point poo, Rgb pathColor, int dir)
        {

            int x = poo.X;
            int y = poo.Y;

            //scan starting from the point of interest into the given dir until the color of that pixel does not longer match the path color
            while (true)
            {
                //check if we ran of the image
                if (x < 0 || x >= img.Width)
                {
                    return x;
                }
                else
                {
                    //check the color
                    if (pathColor.Blue == img[y, x].Blue && pathColor.Red == img[y, x].Red && pathColor.Green == img[y, x].Green)
                    {
                        //still the same color, keep going
                        x = x + dir;
                    }
                    else
                    {
                        //end of the path reached
                        return x - dir;
                    }
                }
            }
        }

        public static bool objectInside(Image<Gray, byte> mask, int scanHeigth)
        {
            for (int i = 0; i < mask.Width; i++)
            {
                bool isOnObject = mask[scanHeigth, i].Intensity > 0;
                if (isOnObject)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool scanAhead(Image<Rgb, byte> img, System.Drawing.Point poo, Image<Rgb, byte> dbImage, Rgb pathColor, ref System.Drawing.Point pointOfIntersection, out int dir)
        {
            int scanInterval = 15;

            //stuff.SetImage(img);

            dir = 0;

            int y = poo.Y;
            int x = poo.X;
            int stepsTillLastHorizontalScan = 0;
            int pathEnd = 0;

            pathColor = img[poo];

            //scan verticaly until the path ends
            while (true)
            {
                //check if we ran of the image
                if (y < 0)
                {
                    //ran of the image
                    break;
                }
                else
                {

                    //check if the color is still the same
                    if (pathColor.Blue == img[y, x].Blue && pathColor.Red == img[y, x].Red && pathColor.Green == img[y, x].Green)
                    {
                        //still on the path
                        //check if we need to start a horizontal scan
                        if (stepsTillLastHorizontalScan == scanInterval)
                        {
                            //start an horizontal scan
                            System.Drawing.Point origin = new System.Drawing.Point(x, y);
                            int pathEndLeft = scanHorizontal(img, origin, pathColor, -1);
                            int pathEndRigth = scanHorizontal(img, origin, pathColor, 1);

                            LineSegment2D ls2 = new LineSegment2D(new System.Drawing.Point(pathEndLeft, y), new System.Drawing.Point(pathEndRigth, y));


                            //check if an intersection
                            if (pathEndLeft == 0 || pathEndRigth == img.Width)
                            {

                                pointOfIntersection.X = origin.X;
                                pointOfIntersection.Y = origin.Y;

                                //chck which way the intersection
                                if (pathEndLeft == 0)
                                {
                                    dir = -1;
                                }
                                else
                                {
                                    dir = 1;
                                }

                                dbImage.Draw(ls2, new Rgb(0, 255, 0), 2);
                                return true;

                            }
                            else
                            {
                                dbImage.Draw(ls2, new Rgb(0, 0, 255), 2);
                            }


                            //reset the counter
                            stepsTillLastHorizontalScan = 0;
                        }
                        else
                        {
                            stepsTillLastHorizontalScan++;
                        }
                        y--;
                    }
                    else
                    {
                        //path endet
                        pathEnd = y;
                        break;
                    }

                }
            }
            LineSegment2D ls3 = new LineSegment2D(new System.Drawing.Point(poo.X, poo.Y), new System.Drawing.Point(poo.X, pathEnd));
            dbImage.Draw(ls3, new Rgb(0, 0, 255), 2);
            return false;
        }

        public static bool isNotObject(Image<Rgb, byte> img, Rgb pathColor, int x, int y)
        {
            int horizonThreshold = (img.Height / 2) - 10;

            for (int i = y; i > horizonThreshold; i--)
            {
                if (pathColor.Blue == img[i, x].Blue && pathColor.Red == img[i, x].Red && pathColor.Green == img[i, x].Green)
                {
                    
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isSidePath(int lineStart, int lineEnd, int imgWidth)
        {
            //first case applies if the path is wider than the image
            if (lineStart == 0 && lineEnd == imgWidth)
            {
                return true;
            }
            else if (lineStart > 0 && lineEnd <= imgWidth)
            {
                return true;
            }

            return false;
        }

        public static bool scanForPath(Image<Rgb, byte> img, int heigth, out int lineStart, out int lineEnd, int pathMinThreshold)
        {

            lineStart = 0;
            lineEnd = 256;

            // ...and find the line
            for (int x = 0; x < img.Width; ++x)
            {

                //color to check the path
                Rgb pixel = img[heigth, x];
                int puixelOfSameColor = 0;
                lineStart = x;

                //look ahead to see if this is the color of tzhe path
                for (int j = x; j < img.Width; j++)
                {
                    if (pixel.Blue == img[heigth, j].Blue && pixel.Red == img[heigth, j].Red && pixel.Green == img[heigth, j].Green)
                    {
                        puixelOfSameColor++;
                        lineEnd = j;
                    }
                    else
                    {
                        lineEnd = j - 1;
                        break;
                    }
                }



                //cherck if this was the path
                if (puixelOfSameColor >= pathMinThreshold)
                {
                    return true;
                }

            }

            return false;
        }

       /* public static double getDistance(Image<Gray, byte> disp, Image<Gray, byte> center, FrmImage dbImage)
        {
            Image<Gray, byte> distImg = center.Copy();

            int heigth = 256 / 2;
            byte distThres = 255 - 30;

            double avrgDist = 0;
            int valuesAdded = 0;

            int sideSpacing = 80;

            for (int i = sideSpacing; i < distImg.Width - sideSpacing; i++)
            {
                //get the distance
                //byte distance = disp.Data[heigth, i, 0];

                double distance = _stereoVision.GetDepth(i, heigth);

                avrgDist += distance;
                valuesAdded++;


                //Logger.Instance.LogInfo("d: "+distance);
                if (distance <= distThres)
                {
                    distImg[heigth, i] = new Gray(255);
                }
                else
                {
                    distImg[heigth, i] = new Gray(0);
                }


            }

            dbImage.SetImage(distImg);
            return avrgDist / valuesAdded;
        }*/


       /* public static double getDistance(Image<Gray, short> disp, Image<Rgb, byte> center, FrmImage dbImage)
        {
            Image<Rgb, byte> distImg = center.Copy();

            int heigth = 256 / 2;
            short distThres = 255 - 30;

            double avrgDist = 0;
            int valuesAdded = 0;

            int sideSpacing = 80;

            for (int i = sideSpacing; i < distImg.Width - sideSpacing; i++)
            {
                //get the distance
                //short distance = disp.Data[heigth, i, 0];

                double distance = _stereoVision.GetDepth(i, heigth);

                avrgDist += distance;
                valuesAdded++;


                //Logger.Instance.LogInfo("d: "+distance);
                if (distance <= distThres)
                {
                    distImg[heigth, i] = new Rgb(255, 0, 0);
                }
                else
                {
                    distImg[heigth, i] = new Rgb(0, 255, 0);
                }


            }

            dbImage.SetImage(distImg);
            return avrgDist / valuesAdded;
        }*/


        public static int getSegments(Image<Gray, short> disp, Image<Rgb, byte> center, FrmImage dbImage, out List<Segment> objectSegments, short distThres, int heigth)
        {
            Image<Rgb, byte> distImg = center.Copy();


            int avrgDist = 0;
            int valuesAdded = 0;

            objectSegments = new List<Segment>();
            int newSegemntStart = 0;

            bool onObject = distThres < disp.Data[heigth, 0, 0];



            for (int i = 0; i < distImg.Width; i++)
            {
                //get the distance
                short distance = disp.Data[heigth, i, 0];

                // double distance = _stereoVision.GetDepth(i, heigth);

                avrgDist += distance;
                valuesAdded++;


                bool isCurrentPixelObject = distThres < distance;


                //check if the current pixel still the same as the segment we are currently tracking
                if (isCurrentPixelObject == onObject)
                {
                    //in this case do nothing
                }
                else
                {
                    //change detected
                    objectSegments.Add(new Segment(newSegemntStart, i - 1, onObject));
                    newSegemntStart = i;
                    onObject = isCurrentPixelObject;

                }


                //print to the db image
                if (isCurrentPixelObject)
                {
                    //distImg[heigth, i] = new Rgb(255, 0, 0);
                }
                else
                {
                    //distImg[heigth, i] = new Rgb(0, 255, 0);
                }

            }

            //dont forget to add the last segment
            objectSegments.Add(new Segment(newSegemntStart, distImg.Width, onObject));

            dbImage.SetImage(distImg);
            return avrgDist / valuesAdded;
        }




        public static bool checkIfvalidPath(int start, int end, int heigth, Image<Rgb, byte> img, int scanDist)
        {
            int center = (end - ((end - start) / 2));

            Rgb referenceColor = img[heigth, center];

            bool isObj = isNotObject(img, referenceColor, center, heigth);

            if (!isObj)
            {
                return false;
            }

            for (int i = 0; i < scanDist; i++)
            {
                if (referenceColor.Blue == img[heigth - i, center].Blue && referenceColor.Red == img[heigth - i, center].Red && referenceColor.Green == img[heigth - i, center].Green)
                {

                }
                else
                {
                    LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(center, heigth), new System.Drawing.Point(center, heigth - i));

                    img.Draw(ls, new Rgb(0, 255, 0), 2);

                    return false;
                }
            }

            LineSegment2D ls1 = new LineSegment2D(new System.Drawing.Point(center, heigth), new System.Drawing.Point(center, heigth - scanDist));

            img.Draw(ls1, new Rgb(0, 0, 255), 2);
            return true;
        }

        public static bool scanForSidePathRigth(Image<Rgb, byte> img, int scanHeigth, int minSegmentLength)
        {

            int sameColorPixels = 0;

            Rgb pathColor = img[scanHeigth, 0];


            //the path we are intereted in should start at the rigth side of the image, if the path would lie in the center of the image the normal side image eval algo would have detected that path
            for (int i = 0; i < img.Width; i++)
            {
                //check whether the pixel still is of the color we are looking for
                if (pathColor.Blue == img[scanHeigth, i].Blue && pathColor.Red == img[scanHeigth, i].Red && pathColor.Green == img[scanHeigth, i].Green)
                {
                    sameColorPixels++;
                }
                else
                {
                    //a different color was scanned, check if we saw enough pixel
                    if (sameColorPixels >= minSegmentLength)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }

            //realisticly this should never be reached
            return false;
        }


        public static bool scanForSidePathLeft(Image<Rgb, byte> img, int scanHeigth, int minSegmentLength)
        {
            int sameColorPixels = 0;

            Rgb pathColor = img[scanHeigth, img.Width - 1];


            //the path we are intereted in should start at the rigth side of the image, if the path would lie in the center of the image the normal side image eval algo would have detected that path
            for (int i = img.Width - 1; i > 0; i--)
            {
                //check whether the pixel still is of the color we are looking for
                if (pathColor.Blue == img[scanHeigth, i].Blue && pathColor.Red == img[scanHeigth, i].Red && pathColor.Green == img[scanHeigth, i].Green)
                {
                    sameColorPixels++;
                }
                else
                {
                    //a different color was scanned, check if we saw enough pixel
                    if (sameColorPixels >= minSegmentLength)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }

            //realisticly this should never be reached
            return false;
        }
    }
}
