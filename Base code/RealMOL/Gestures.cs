using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace RealMOL
{
    public class Gestures
    {
        //Estructura que guarda las posiciones
        public struct Position
        {
            public float x;
            public float y;
            public float z;
            public DateTime date;
        }

        const int FRAMESFORZOOM = 3; //Frames procesados para un zoom
        const int MAXFRAMES = 3; //Máxima cantidad de frames a almacenar

        const float DISTANCETOLERANCE = 0.1f; //Distancia tolerada para considerar que dos coordenadas están cerca
        const float DISTANCESTEP = 0.01f; //Distancia que define un movimiento de coordenadas entre frames

        static List<Position> rightHandHistory = new List<Position>(); //Lista de posiciones de la mano derecha
        static List<Position> leftHandHistory = new List<Position>(); //Lista de posiciones de la mano izquierda

        /*
         * Función: UpdatePositions
         * Descripción: Función que actualiza las listas de posiciones
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: rightHand (Joint, ultimo nodo de la mano derecha), leftHand (Joint, ultimo nodo de la mano izquierda)
         */
        public static void UpdatePositions(Joint rightHand, Joint leftHand)
        {
            Position newPosition = new Position();
            newPosition.x = rightHand.Position.X;
            newPosition.y = rightHand.Position.Y;
            newPosition.z = rightHand.Position.Z;
            newPosition.date = DateTime.Now;
            rightHandHistory.Add(newPosition);
            newPosition.x = leftHand.Position.X;
            newPosition.y = leftHand.Position.Y;
            newPosition.z = leftHand.Position.Z;
            leftHandHistory.Add(newPosition);
        }

        /*
         * Función: CleanPositions
         * Descripción: Función que limpia las listas de posiciones
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Listas de posiciones limpias
         */
        public static void CleanPositions()
        {
            if (rightHandHistory.Count >= MAXFRAMES)
            {
                rightHandHistory.RemoveAt(0);
            }
            if (leftHandHistory.Count >= MAXFRAMES)
            {
                leftHandHistory.RemoveAt(0);
            }
        }

        /*
         * Función: ZoomIn
         * Descripción: Función que reconoce si los frames corresponden a un gesto de acercamiento
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: (bool, valor que indica si el gesto fue reconocido)
         */
        public static bool ZoomIn()
        {
            int points = 0;
            float actualDistanceX;
            float lastDistanceX;
            if (rightHandHistory.Count >= FRAMESFORZOOM && leftHandHistory.Count >= FRAMESFORZOOM)
            {
                for (int i = 1; i < FRAMESFORZOOM; i++)
                {
                    lastDistanceX = rightHandHistory[i - 1].x - leftHandHistory[i - 1].x;
                    actualDistanceX = rightHandHistory[i].x - leftHandHistory[i].x;
                    if (Math.Abs(rightHandHistory[i].y - leftHandHistory [i].y) < DISTANCETOLERANCE)
                    {
                        points += 2;
                    }
                    if ((lastDistanceX - actualDistanceX) > DISTANCESTEP)
                    {
                        points += 3;
                    }
                }
                if (points >= FRAMESFORZOOM * 3)
                {
                    rightHandHistory.Clear();
                    leftHandHistory.Clear();
                    return true;
                }
            }
            return false;
        }

        /*
         * Función: ZoomOut
         * Descripción: Función que reconoce si los frames corresponden a un gesto de alejamiento
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: (bool, valor que indica si el gesto fue reconocido)
         */
        public static bool ZoomOut()
        {
            int points = 0;
            float actualDistanceY;
            float lastDistanceY;
            if (rightHandHistory.Count >= FRAMESFORZOOM && leftHandHistory.Count >= FRAMESFORZOOM)
            {
                for (int i = 1; i < FRAMESFORZOOM; i++)
                {
                    lastDistanceY = rightHandHistory[i - 1].y - leftHandHistory[i - 1].y;
                    actualDistanceY = rightHandHistory[i].y - leftHandHistory[i].y;
                    if (Math.Abs(rightHandHistory[i].x - leftHandHistory[i].x) < DISTANCETOLERANCE)
                    {
                        points += 2;
                    }
                    if ((actualDistanceY - lastDistanceY) > DISTANCESTEP)
                    {
                        points += 3;
                    }
                }
                if (points >= FRAMESFORZOOM * 3)
                {
                    rightHandHistory.Clear();
                    leftHandHistory.Clear();
                    return true;
                }
            }
            return false;
        }
    }
}
