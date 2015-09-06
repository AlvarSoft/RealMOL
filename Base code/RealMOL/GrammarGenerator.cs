using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Speech.Recognition;

namespace RealMOL
{
    public class GrammarGenerator
    {
        //Gramática que será utilizada para entrenar al Kinect 
        private static Choices grammar;

        public static string UNLOCK_COMMAND = "continuar";

        private static char[] CHARACTERS = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }; //Letras del alfabeto y dígitos numéricos
        public static string[] numbers_sounds = new string[9999];

        public static string[] GEOMETRIC_COMMANDS = { "enfocar", "girar", "mover", "rotar" }; //Comandos de control geométrico
        public static string[] MENU_COMMANDS = { "cancelar", "siguiente", "anterior", "atras" }; //Comandos para navegar por los menús
        public static string[] DICTATION_COMMANDS = { "borrar", "aceptar" }; //Comandos para controlar los dictados
        public static string[] CHARACTERS_SOUNDS = { "vocal a", "letra be", "letra ce", "letra de", "vocal e", "letra efe", "letra ge", "letra ache", "vocal i", "letra jota", "letra ka",
                                                       "letra ele", "letra eme", "letra ene", "vocal o", "letra pe", "letra ku", "letra ere", "letra ese", "letra te", "vocal u", "letra uve", 
                                                       "letra dobleu", "letra equis", "letra igriega", "letra zeta", "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" }; //Representación fonética de las letras del alfabeto y los números del 0 al 9
        public static string[] ALT_CHARACTERS_SOUNDS = { "alfa", "bravo", "charli", "delta", "eco", "foxtrot", "golf", "hotel", "india", "yuliet", "kilo", "lima", "maik", "november", "oscar",
                                                           "papa", "kebek", "romeo", "sierra", "tango", "iuniform", "victor", "wiski", "exrei", "yanki", "zulu" }; //Alfabeto alterno con base en palabras

        private static string[] NUMBERS_UNIT_SOUNDS = { "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" }; //Representación fonética de las unidades
        private static string[] NUMBERS_SPECIAL_SOUNDS = { "diez", "once", "doce", "trece", "catorce", "quince", "dieciseis", "diecisiete", "dieciocho", "diecinueve", "veinte", 
                                                             "veintiuno", "veintidos", "veintitres", "veinticuatro", "veinticinco", "veintiseis", "veintisiete", "veintiocho", "veintinueve" }; //Representación fonética de los números del 10 al 29
        private static string[] NUMBERS_TENS_SOUNDS = { "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa" }; //Representación fonética de las decenas
        private static string[] NUMBERS_HUNDREDS_SOUNDS = { "ciento", "doscientos", "trecientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos" }; //Representación fonética de las centenas
        private static string[] NUMBERS_THOUSANDS_SOUNDS = { "mil", "dos mil", "tres mil", "cuatro mil", "cinco mil", "seis mil", "siete mil", "ocho mil", "nueve mil" }; //Representación fonética de las milésimas

        /*
         * Función: AddNumbers
         * Descripción: Función que agrega el sonido de los números del 1 al 9999 a la gramática
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Gramática actualizada
         */
        private static void AddNumbers()
        {
            int i = 0;
            foreach (string unit in NUMBERS_UNIT_SOUNDS)
            {
                numbers_sounds[i] = unit;
                i++;
            }
            foreach (string special in NUMBERS_SPECIAL_SOUNDS)
            {
                numbers_sounds[i] = special;
                i++;
            }
            foreach (string ten in NUMBERS_TENS_SOUNDS)
            {
                numbers_sounds[i] = ten;
                i++;
                foreach (string unit in NUMBERS_UNIT_SOUNDS)
                {
                    numbers_sounds[i] = ten + " y " + unit;
                    i++;
                }
            }
            foreach (string hundred in NUMBERS_HUNDREDS_SOUNDS)
            {
                if (hundred == "ciento")
                {
                    numbers_sounds[i] = "cien";
                }
                else
                {
                    numbers_sounds[i] = hundred;
                }
                i++;
                foreach (string unit in NUMBERS_UNIT_SOUNDS)
                {
                    numbers_sounds[i] = hundred + " " + unit;
                    i++;
                }
                foreach (string special in NUMBERS_SPECIAL_SOUNDS)
                {
                    numbers_sounds[i] = hundred + " " + special;
                    i++;
                }
                foreach (string ten in NUMBERS_TENS_SOUNDS)
                {
                    numbers_sounds[i] = hundred + " " + ten;
                    i++;
                    foreach (string unit in NUMBERS_UNIT_SOUNDS)
                    {
                        numbers_sounds[i] = hundred + " " + ten + " y " + unit;
                        i++;
                    }
                }
            }
            foreach (string thousand in NUMBERS_THOUSANDS_SOUNDS)
            {
                numbers_sounds[i] = thousand;
                i++;
                foreach (string unit in NUMBERS_UNIT_SOUNDS)
                {
                    numbers_sounds[i] = thousand + " " + unit;
                    i++;
                }
                foreach (string special in NUMBERS_SPECIAL_SOUNDS)
                {
                    numbers_sounds[i] = thousand + " " + special;
                    i++;
                }
                foreach (string ten in NUMBERS_TENS_SOUNDS)
                {
                    numbers_sounds[i] = thousand + " " + ten;
                    i++;
                    foreach (string unit in NUMBERS_UNIT_SOUNDS)
                    {
                        numbers_sounds[i] = thousand + " " + ten + " y " + unit;
                        i++;
                    }
                }
                foreach (string hundred in NUMBERS_HUNDREDS_SOUNDS)
                {
                    if (hundred == "ciento")
                    {
                        numbers_sounds[i] = thousand + " cien";
                    }
                    else
                    {
                        numbers_sounds[i] = thousand + " " + hundred;
                    }
                    i++;
                    foreach (string unit in NUMBERS_UNIT_SOUNDS)
                    {
                        numbers_sounds[i] = thousand + " " + hundred + " " + unit;
                        i++;
                    }
                    foreach (string special in NUMBERS_SPECIAL_SOUNDS)
                    {
                        numbers_sounds[i] = thousand + " " + hundred + " " + special;
                        i++;
                    }
                    foreach (string ten in NUMBERS_TENS_SOUNDS)
                    {
                        numbers_sounds[i] = thousand + " " + hundred + " " + ten;
                        i++;
                        foreach (string unit in NUMBERS_UNIT_SOUNDS)
                        {
                            numbers_sounds[i] = thousand + " " + hundred + " " + ten + " y " + unit;
                            i++;
                        }
                    }
                }
            }
            foreach (string numberSound in numbers_sounds)
            {
                grammar.Add(numberSound);
            }
        }

        /*
         * Función: AddNodeToGrammar
         * Descripción: Función recursiva que recorre el árbol de comandos y agrega los textos de menú y comando a la gramática
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: Nodo inicial
         * Salidas: Gramática actualizada
         */
        private static void AddNodeToGrammar(CommandNode node)
        {
            if (node == null)
            {
                Console.WriteLine("El árbol de comandos no se inicializo antes de crear la gramática");
                return;
            }
            if (node.type == Types.Command)
            {
                grammar.Add(node.text);
            }
            else if (node.type == Types.Menu)
            {
                grammar.Add(node.text);
                foreach (CommandNode child in node.children)
                {
                    AddNodeToGrammar(child);
                }
            }
            else
            {
                foreach (CommandNode child in node.children)
                {
                    AddNodeToGrammar(child);
                }
            }
        }

        /*
         * Función: GetGrammar
         * Descripción: Función que llena y devuelve la gramática usando los vectores de comandos y el árbol de comandos
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: Nodo inicial del árbol de comandos
         * Salidas: (Choices, gramática para entrenar a Kinect)
         */
        public static Choices GetGrammar(CommandNode commandTree)
        {
            grammar = new Choices();
            grammar.Add(UNLOCK_COMMAND);
            foreach (string command in GEOMETRIC_COMMANDS)
            {
                grammar.Add(command);
            }
            foreach (string command in MENU_COMMANDS)
            {
                grammar.Add(command);
            }
            foreach (string command in DICTATION_COMMANDS)
            {
                grammar.Add(command);
            }
            foreach (string characterSound in CHARACTERS_SOUNDS)
            {
                grammar.Add(characterSound);
            }
            foreach (string characterSound in ALT_CHARACTERS_SOUNDS)
            {
                grammar.Add(characterSound);
            }
            AddNodeToGrammar(commandTree);
            AddNumbers();
            return grammar;
        }

        /*
         * Función: GetChar
         * Descripción: Función que obtiene el carácter de una representación fonética
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: Sound (string, representación fonética de la letra)
         * Salidas: (char, carácter correspondiente a la entrada, si es inválida entonces se regresa un fin de cadena)
         */
        public static char GetChar(string sound)
        {
            int index = Array.IndexOf(CHARACTERS_SOUNDS, sound);
            if (index >= 0)
            {
                return CHARACTERS[index];
            }
            index = Array.IndexOf(ALT_CHARACTERS_SOUNDS, sound);
            if (index >= 0)
            {
                return CHARACTERS[index];
            }
            return '\0';
        }

        /*
         * Función: GetNumber
         * Descripción: Función que obtiene el carácter de una representación fonética
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: Sound (string, representación fonética del número)
         * Salidas: (string, número correspondiente a la entrada, si es inválida entonces se regresa una cadena vacía)
         */
        public static string GetNumber(string sound)
        {
            int index = Array.IndexOf(numbers_sounds, sound);
            if (index >= 0)
            {
                return (index + 1).ToString();
            }
            return "";
        }
    }
}
