using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;
using Emgu.CV.Structure;
using GLab.Core;

namespace Frame.VrAibo
{

    abstract class State
    {
        public string stateName;
        public List<State> connectedStates;       

        abstract public State calcNextTurn(GLab.StereoVision.StereoVision stereoVision, GLab.VirtualAibo.VrAibo vrAibo, Image<Rgb, byte> img, Image<Rgb, byte> dbImg);

        public State goToState(string name)
        {
            for (int i = 0; i < connectedStates.Count; i++)
            {
                if (connectedStates[i].stateName == name)
                {
                    return connectedStates[i];
                }
            }

            return null;
        }
    }


    class DecideIntersection : State
    {



        public bool[] directions;
        public int dirs = 0;
        private bool turned = false;
        private Rgb pathColor;


        public DecideIntersection()
        {
            stateName = "Decide intersection";
            connectedStates = new List<State>();
        }

        public override State calcNextTurn(GLab.StereoVision.StereoVision stereoVision, GLab.VirtualAibo.VrAibo vrAibo, Image<Rgb, byte> img, Image<Rgb, byte> dbImg)
        {
            if (!turned)
            {             

                vrAibo.Turn(-45);
                turned = true;
            }
            
            Logger.Instance.LogInfo("waiting to make a decision at intersection, dirs are " + dirs);
            return this;
        }
    }

    class ApproachIntersection : State
    {

        private const float Alpha = GLab.VirtualAibo.VrAibo.FovY / GLab.VirtualAibo.VrAibo.SurfaceWidth;

        public int dir =0;

        public ApproachIntersection()
        {
            stateName = "Intersection approach";
            connectedStates = new List<State>();
        }

        public override State calcNextTurn(GLab.StereoVision.StereoVision stereoVision, GLab.VirtualAibo.VrAibo vrAibo, Image<Rgb, byte> img, Image<Rgb, byte> dbImg)
        {
            int lineStart = -1;
            int lineEnd = -1;

            int scanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 40;

            ImageOperations.scanForPath(img, scanHeigth, out lineStart, out lineEnd, 50);

            if (lineStart != -1 && lineEnd != -1)
            {
                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStart, scanHeigth), new System.Drawing.Point(lineEnd, scanHeigth));


                int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEnd - ((lineEnd - lineStart) / 2));
                float phi = Alpha * diffX;

                //if (phi != 0.0f)
                //Logger.Instance.LogInfo("Turning by " + phi + " degree");

                //turn = phi / 2;


                int pathCenter = (lineEnd - ((lineEnd - lineStart) / 2));
                System.Drawing.Point pointOfOrigin = new System.Drawing.Point(pathCenter, scanHeigth);

                //check for an intersection or turn
                System.Drawing.Point possibleIntersection = new System.Drawing.Point(0, 0);
                int dirOfIntersection = 0;

                if (ImageOperations.scanAhead(img, pointOfOrigin, dbImg, img[pointOfOrigin], ref possibleIntersection, out dirOfIntersection,5,43))
                {
                    //intersection or turn found
                    //calc the distance to the intersection
                    double distToIntersection = stereoVision.GetDepth(possibleIntersection.X, possibleIntersection.Y);
                    //move up to the intersection

                    distToIntersection = Math.Abs(distToIntersection);

                    float d = Convert.ToSingle(distToIntersection);

                    if (phi != 0.0f)
                    {
                        vrAibo.Turn(phi / 2);
                    }
                    vrAibo.Walk(d);

                    dbImg.Draw(ls, new Rgb(0, 0, 255), 2);

                    Logger.Instance.LogInfo("still approaching");

                    return this;


                }
                else
                {

                    //reached the intersection


                    if (phi != 0.0f)
                    {
                        vrAibo.Turn(phi / 2);
                    }

                    vrAibo.Walk(0.3f);

                    DecideIntersection di = goToState("Decide intersection") as DecideIntersection;
                    di.dirs = dir;

                    dbImg.Draw(ls, new Rgb(0, 0, 255), 2);
                    Logger.Instance.LogInfo("switching to decide");
                    return di;
                }

            }
            else
            {
                //no line found
                return this;
            }
        }
    }



    class FollowPathState : State
    {

        private const float Alpha = GLab.VirtualAibo.VrAibo.FovY / GLab.VirtualAibo.VrAibo.SurfaceWidth;

        public FollowPathState()
        {
            stateName = "Follow path";
            connectedStates = new List<State>();
        }

        public override State calcNextTurn(GLab.StereoVision.StereoVision stereoVision, GLab.VirtualAibo.VrAibo vrAibo, Image<Rgb, byte> img,Image<Rgb, byte> dbImg)
        {
            int lineStart = -1;
            int lineEnd = -1;
            
            int scanHeigth = GLab.VirtualAibo.VrAibo.SurfaceHeight - 40;

            ImageOperations.scanForPath(img, scanHeigth, out lineStart, out lineEnd,50);

            if (lineStart != -1 && lineEnd != -1)
            {
                LineSegment2D ls = new LineSegment2D(new System.Drawing.Point(lineStart, scanHeigth), new System.Drawing.Point(lineEnd, scanHeigth));


                int diffX = (GLab.VirtualAibo.VrAibo.SurfaceWidth / 2) - (lineEnd - ((lineEnd - lineStart) / 2));
                float phi = Alpha * diffX;

                //if (phi != 0.0f)
                //Logger.Instance.LogInfo("Turning by " + phi + " degree");

                //turn = phi / 2;


                int pathCenter = (lineEnd - ((lineEnd - lineStart) / 2));


                System.Drawing.Point pointOfOrigin = new System.Drawing.Point(pathCenter, scanHeigth);


                //check for an intersection or turn
                System.Drawing.Point possibleIntersection = new System.Drawing.Point(0, 0);
                int dirOfIntersection = 0;

                if (ImageOperations.scanAhead(img, pointOfOrigin, dbImg, img[pointOfOrigin], ref possibleIntersection, out dirOfIntersection,10,35))
                {
                    //intersection or turn found
                    //calc the distance to the intersection
                    double distToIntersection = stereoVision.GetDepth(possibleIntersection.X, possibleIntersection.Y);
                    //move up to the intersection

                    ApproachIntersection appInter = goToState("Intersection approach") as ApproachIntersection;

                    appInter.dir = dirOfIntersection;


                    distToIntersection = Math.Abs(distToIntersection);                   

                    float d = Convert.ToSingle(distToIntersection);

                    if (phi != 0.0f)
                    {
                        vrAibo.Turn(phi / 2);
                    }
                    vrAibo.Walk(d);

                    dbImg.Draw(ls, new Rgb(0, 0, 255), 2);

                    Logger.Instance.LogInfo("changing to intersection approach");

                    return appInter;


                }
                else
                {

                    if (phi != 0.0f)
                    {
                        vrAibo.Turn(phi / 2);
                    }

                    vrAibo.Walk(0.3f);


                    dbImg.Draw(ls, new Rgb(0, 0, 255), 2);
                    Logger.Instance.LogInfo("still follow path");
                    return this;
                }              

            }
            else
            {
                //no line found
                return this;
            }
        }
    }

    class StateMachine
    {
        public List<State> allStates;
        public State currentState;
        public StateMachine()
        {
            allStates = new List<State>();
            addStates();
        }

        public void makeMove(GLab.StereoVision.StereoVision stereoVision, GLab.VirtualAibo.VrAibo vrAibo, Image<Rgb, byte> img, Image<Rgb, byte> dbImg)
        {
            State nextState = currentState.calcNextTurn(stereoVision, vrAibo, img, dbImg);
            currentState = nextState;
        }

        public void addStates()
        {
            allStates.Add(new FollowPathState());
            currentState = allStates[0];
            ApproachIntersection appInt = new ApproachIntersection();

            allStates[0].connectedStates.Add(appInt);
            allStates.Add(appInt);

            DecideIntersection di = new DecideIntersection();
            allStates[1].connectedStates.Add(di);
            allStates.Add(di);

        }

    }


}
