using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GLab.Core;
using Emgu.CV;
using Microsoft.Xna.Framework;

namespace Frame.VrAibo
{
	public class Obstacle
	{
		public Emgu.CV.Structure.Hsv obstacleColor;
		public Vector2 obstaclePos;
        public bool isOfInterest;


		public Obstacle()
		{
			obstacleColor = new Emgu.CV.Structure.Hsv(0, 0, 0);
			obstaclePos = new Vector2(0, 0);
            isOfInterest = false;
		}

		public Obstacle(Emgu.CV.Structure.Hsv color, Vector2 pos)
		{
			obstacleColor = color;
			obstaclePos = pos;
            isOfInterest = false;
		}
		
	}

	public class ObstacleManager
	{
		private List<Obstacle> knowObstacals;

		private double minObjectDistance = 10;

		private double minColorDist = 5;

		public ObstacleManager()
		{
			knowObstacals = new List<Obstacle>();
		}

		public void addObstacal(Emgu.CV.Structure.Hsv color, Vector2 pos)
		{
			//check if the object is already known by checking if we know of an object that is very close to this one
			for (int i = 0; i < knowObstacals.Count; i++)
			{
				//first check the color
				double colorDist = Math.Abs(color.Hue - knowObstacals[i].obstacleColor.Hue);
				if (colorDist < minColorDist)
				{
					//color is within range
					double distance = calcDistance(pos, knowObstacals[i].obstaclePos);
					if (distance < minObjectDistance)
					{
					  
						return;
					}

				}
			}

			knowObstacals.Add(new Obstacle(color, pos));

		}

		private double calcDistance(Vector2 v1, Vector2 v2)
		{
			return  Math.Sqrt(Math.Pow((v1.X - v2.X), 2) + Math.Pow((v1.Y - v2.Y), 2));
		}

        //public List<Emgu.CV.Structure.Hsv> getColorsOfObjectsInView(Vector2 refPos,double distanceThreshold,)

		public List<Emgu.CV.Structure.Hsv> getColorsOfCloseObstacals(Vector2 refPos, double distanceThreshold)
		{
			List<Emgu.CV.Structure.Hsv> colorsOfCloseObjects =new List<Emgu.CV.Structure.Hsv>();

            //Logger.Instance.LogInfo("--------------------------");
			for (int i = 0; i < knowObstacals.Count; i++)
			{
				double dist = calcDistance(refPos,knowObstacals[i].obstaclePos);

                //objects that are of interest are moved closer
                if (knowObstacals[i].isOfInterest)
                {
                    dist = dist / 2;


                }

                //Logger.Instance.LogInfo("dist to object: "+dist);
				if (dist <= distanceThreshold)
				{
					colorsOfCloseObjects.Add(knowObstacals[i].obstacleColor);
                    knowObstacals[i].isOfInterest = true;
                }
                else
                {
                    knowObstacals[i].isOfInterest = false;
                    //objects that are that too far away are of no interest
                }
			}

            //Logger.Instance.LogInfo("--------------------------");

			return colorsOfCloseObjects;
		}
	}
}
