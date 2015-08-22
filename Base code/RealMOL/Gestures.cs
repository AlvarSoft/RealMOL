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
        const float POSITIVEDISTANCE = 0.15f; //Distancia que debe alejarse la mano para considerar que esta en la zona positiva
        const float POSITIVEFARDISTANCE = 0.35f; //Distancia extra que debe alejarse la mano para considerar que esta en la zona positiva lejana
        const float NEGATIVEDISTANCE = 0.10f; //Distancia que debe alejarse la mano para considerar que esta en la zona negativa
        const float NEGATIVEFARDISTANCE = 0.30f; //Distancia extra que debe alejarse la mano para considerar que esta en la zona negativa lejana

        /*
         * Función: Get2AxisValue
         * Descripción: Función que devuelve los valores de movimiento en 2 ejes
         * Autor: Christian Vargas
         * Fecha de creación: 19/08/15
         * Fecha de modificación: --/--/--
         * Entradas: mainHand (Joint, nodo de la mano principal del usuario), secondaryHand (Joint, nodo de la mano secundaria del usuario), mainShoulder (Joint, nodo del hombro principal del usuario), head (Joint, nodo de la cabeza el usuario)
         * Salidas: (Tuple<bool, int, int>, valor que indica si el movimiento continua y los valores de ambos ejes)
         */
        public static Tuple<bool, int, int> Get2AxisValue(Joint mainHand, Joint secondaryHand, Joint mainShoulder, Joint head)
        {
            int xMovement = 0;
            int yMovement = 0;
            bool moving = true;
            if (secondaryHand.Position.Y >= head.Position.Y)
            {
                moving = false;
            }
            else
            {
                if (mainHand.Position.X >= mainShoulder.Position.X + POSITIVEDISTANCE + POSITIVEFARDISTANCE)
                {
                    xMovement = 2;
                }
                else if (mainHand.Position.X >= mainShoulder.Position.X + POSITIVEDISTANCE)
                {
                    xMovement = 1;
                }
                else if (mainHand.Position.X + NEGATIVEDISTANCE + NEGATIVEFARDISTANCE < mainShoulder.Position.X)
                {
                    xMovement = -2;
                }
                else if (mainHand.Position.X + NEGATIVEDISTANCE < mainShoulder.Position.X)
                {
                    xMovement = -1;
                }
                if (mainHand.Position.Y >= mainShoulder.Position.Y + POSITIVEDISTANCE + POSITIVEFARDISTANCE)
                {
                    yMovement = -2;
                }
                else if (mainHand.Position.Y >= mainShoulder.Position.Y + POSITIVEDISTANCE)
                {
                    yMovement = -1;
                }
                else if (mainHand.Position.Y + NEGATIVEDISTANCE + NEGATIVEFARDISTANCE < mainShoulder.Position.Y)
                {
                    yMovement = 2;
                }
                else if (mainHand.Position.Y + NEGATIVEDISTANCE < mainShoulder.Position.Y)
                {
                    yMovement = 1;
                }
            }
            return Tuple.Create(moving, xMovement, yMovement);
        }

        /*
         * Función: Get1AxleValue
         * Descripción: Función que devuelve el valor de movimiento en 1 eje
         * Autor: Christian Vargas
         * Fecha de creación: 19/08/15
         * Fecha de modificación: --/--/--
         * Entradas: mainHand (Joint, nodo de la mano principal del usuario), secondaryHand (Joint, nodo de la mano secundaria del usuario), mainShoulder (Joint, nodo del hombro principal del usuario), head (Joint, nodo de la cabeza el usuario)
         * Salidas: (Tuple<bool, int>, valor que indica si el movimiento continua y el valor del eje)
         */
        public static Tuple<bool, int> Get1AxleValue(Joint mainHand, Joint secondaryHand, Joint mainShoulder, Joint head)
        {
            int xMovement = 0;
            bool moving = true;
            if (secondaryHand.Position.Y >= head.Position.Y)
            {
                moving = false;
            }
            else
            {
                if (mainHand.Position.X >= mainShoulder.Position.X + POSITIVEDISTANCE + POSITIVEFARDISTANCE)
                {
                    xMovement = 2;
                }
                else if (mainHand.Position.X >= mainShoulder.Position.X + POSITIVEDISTANCE)
                {
                    xMovement = 1;
                }
                else if (mainHand.Position.X + NEGATIVEDISTANCE + NEGATIVEFARDISTANCE < mainShoulder.Position.X)
                {
                    xMovement = -2;
                }
                else if (mainHand.Position.X + NEGATIVEDISTANCE < mainShoulder.Position.X)
                {
                    xMovement = -1;
                }
            }
            return Tuple.Create(moving, xMovement);
        }
    }
}
