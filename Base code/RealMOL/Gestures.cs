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
        public enum PoseTypes { Positive, Negative, Rest, Finish }; //Tipos de pose para la mano

        const float DISTANCETOLERANCE = 0.20f; //Distancia tolerada para considerar que dos coordenadas están cerca

        /*
         * Función: GetHandPose
         * Descripción: Función que devuelve la pose de la mano derecha
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: (PoseTypes, valor que indica en que pose se encuentra la mano deecha)
         */
        public static PoseTypes GetHandPose(Joint rightHand, Joint shoulderCenter, Joint head)
        {
            if (rightHand.Position.Y > head.Position.Y)
            {
                return PoseTypes.Finish;
            }
            if (Math.Abs(rightHand.Position.X - shoulderCenter.Position.X) < DISTANCETOLERANCE &&
                Math.Abs(rightHand.Position.Y - shoulderCenter.Position.Y) < DISTANCETOLERANCE)
            {
                return PoseTypes.Rest;
            }
            if (rightHand.Position.X > shoulderCenter.Position.X)
            {
                return PoseTypes.Positive;
            }
            else
            {
                return PoseTypes.Negative;
            }
        }
    }
}
