using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;
using Emgu.CV.Structure;

namespace Frame.VrAibo
{
    class ImageOperations
    {


        public static void evalImageSet(Image<Rgb, byte> front,Image<Rgb, byte> left,Image<Rgb, byte> rigth){

        }

        public static void scanForPath(Image<Rgb, byte> img, int heigth, out int lineStart, out int lineEnd, int pathMinThreshold)
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
                    return;
                }

            }

            return;
        }


        private static int scanHorizontal(Image<Rgb, byte> img, System.Drawing.Point poo, Rgb pathColor, int dir)
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

        public static bool scanAhead(Image<Rgb, byte> img, System.Drawing.Point poo, Image<Rgb, byte> dbImage, Rgb pathColor, ref System.Drawing.Point pointOfIntersection, out int dir, int scanInterval, int maxLookAhead)
        {                   

            dir = 0;

            int y = poo.Y;
            int x = poo.X;
            int stepsTillLastHorizontalScan = 0;
            int pathEnd = 0;
            int lookedAhead = 0;

            pathColor = img[poo];

            //scan verticaly until the path ends
            while (true)
            {
                //check if we ran of the image or reached the max look ahead value
                if (y < 0 || lookedAhead >= maxLookAhead)
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
                                if (pathEndLeft == 0 && pathEndRigth == img.Width)
                                {
                                    dir = 0;
                                }else if (pathEndLeft == 0)
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
                        lookedAhead++;
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

    }



}
