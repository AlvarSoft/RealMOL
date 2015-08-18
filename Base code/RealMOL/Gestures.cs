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
        public enum PoseTypes { PositiveSlow, NegativeSlow, PositiveFast, NegativeFast, Rest, Finish }; //Tipos de pose para la mano

        const float DISTANCETOLERANCE = 0.20f; //Distancia tolerada para considerar que dos coordenadas están cerca
        const float FARDISTANCE = 0.35f; //Distancia usada para considerar si un punto está lejos

        /*
         * Función: GetHandPose
         * Descripción: Función que devuelve la pose de la mano derecha
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: (PoseTypes, valor que indica en que pose se encuentra la mano deecha)
         */
        public static PoseTypes GetHandPose(Joint rightHand, Joint rightShoulder, Joint head)
        {
            if (rightHand.Position.Y > head.Position.Y)
            {
                return PoseTypes.Finish;
            }
            else if (Math.Abs(rightHand.Position.X - rightShoulder.Position.X) < DISTANCETOLERANCE)
            {
                return PoseTypes.Rest;
            }
            else if (rightHand.Position.X > rightShoulder.Position.X)
            {
                if (rightHand.Position.X - rightShoulder.Position.X >= FARDISTANCE)
                {
                    return PoseTypes.PositiveFast;
                }
                else
                {
                    return PoseTypes.PositiveSlow;
                }
            }
            else
            {
                if (rightShoulder.Position.X -  rightHand.Position.X >= FARDISTANCE)
                {
                    return PoseTypes.NegativeFast;
                }
                else
                {
                    return PoseTypes.NegativeSlow;
                }
            }
        }
    }
}
