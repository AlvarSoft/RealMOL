using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Media;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect;
using SharpDX.XInput;

namespace RealMOL
{
    public partial class Form_Main : Form
    {
        public Form_Main()
        {
            InitializeComponent();
        }

        private const uint XBOXCONTROLDEADZONE = 15000; //Rango de valores que son ignorados de la palanca del control de Xbox 360
        private const uint MAXTHUMBVAL = 32767; //Valor máximo devuelto por la palanca del control de Xbox 360

        private const string IP = "127.0.0.1"; //Dirección IP local
        private const int OUT_PORT = 5005; //Puerto por donde se enviaran los comandos a PyMOL
        private const int IN_PORT = 5006; //Puerto donde se recibiran las respuestas de los comando PyMOL
        private IPEndPoint endPoint; //Punt donde el Servidor de conectará
        private UdpClient udpClient; //Cliente que enviara los comandos a PyMOL
        private UdpClient udpServer; //Cliente que recibirá las respuestas de los comandos PyMOL
        private Byte[] sendBytes; //Cadena de Bytes que se enviara con los comandos a PyMOL
        private Byte[] recvBytes; //Cadena de Bytes que recibirá las respuestas de PyMOL
        private CommandNode commandTree; //Raíz del árbol de comandos, el árbol de comandos guarda todos menús validos junto con los códigos reconocidos por PyMOL
        private string newCommand = ""; //Variable que guarda los comandos que el usuario ha dicho y que aún no llegan a una terminación
        private string molCode = ""; //Variable que guarda el código de una molécula que es dictada
        private string selName = ""; //Variable que guarda el nombre de la selección que es dictada
        private string resISel = ""; //Variable que guarda los enteros de la selección que está siendo dictada
        private string fontSize = ""; //Variable que guarda el tamaño de la fuente que está siendo dictada
        private int menuPage = 1; //Variable que guarda la página actual en la que se encuentra un menú 
        private bool showingTitles = false; //Variable que permite saber si actualmente se están mostrando los títulos de las moleculas
        private bool hearingMol = false; //Variable que permite saber si actualmente se está escuchando una molécula
        private bool hearingSel = false; //Variable que permite saber si actualmente se está escuchando el nombre de una selección
        private bool hearingResI = false; //Variable que permite saber si actualmente se están escuchando los enteros de una selección
        private bool hearingFontSize = false; //Variable que permite saber si actualmente se está escuchando el tamaño de una fuente
        private bool displayingRayWarning = false; //Variable que permite saber si actualmente se está mostrando una advertencia de ray
        private bool displayingMolList = false; //Variable que permite saber si actualmente se está mostrando la lista de moléculas
        private bool blocked = false; //Variable que indica si la entrada de comandos se encuentra actualmente bloqueada

        private List<string> downloadedMol; //Arreglo donde se guardan las moléculas descargadas
        private List<string> selectedMol; //Arrgelo donde se guardan las moléculas seleccionadas

        private Controller xboxControl; //Control de Xbox 360
        private uint xboxControlSensitivity = 3; //Valor de sensibilidad para el control de Xbox 360
        private bool xboxControlDetected = false; //Variable que determinara si el control de Xbox 360 fue detectado antes de iniciar PyMOL

        private KinectSensor sensor; //Sensor Kinect
        private SpeechRecognitionEngine speechRecognizer; //Motor de reconocimiento de voz
        private bool waiting = false; //Variable que determina si Kinect está esperando un comando de voz
        private const float CONFIDENCE = 0.6f; //Valor de confianza mínimo para aceptar un comando de voz

        private bool geometricWaiting = false; //Variable que indica si actualmente se está llevando a cabo un comando geométrico
        private string geometricCommand = ""; //Comando geométrico actual

        private const int SLEEPTIME = 150; //Tiempo de espera entre lectura de botones del control de Xbox 360

        private const int MAXLIST = 5; //Cantidad máxima de elementos que se muestran en una lista

        /*
         * Función: GetKinectRecognizer
         * Descripción: Función que busca el reconocedor de voz Kinect con el paquete de idioma en español mexicano
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: recognizer (RecognizerInfo, Reconocedor de voz de Kinect)
         * Notas: Si no se logra encontrar el reconocedor, el valor retornado es null
         */
        private static RecognizerInfo GetKinectRecognizer()
        {
            //Se recorren todos los reconocedores instalados, buscando aquel cuya información de cultura sea de México, se regresa el primero en ser encontrado
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                if ("es-MX".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            //Si no se encontró ningún reconocedor con información cultural de México, se regresa null
            return null;
        }

        /*
         * Función: CreateSpeechRecognizer
         * Descripción: Función que crea el motor de reconocimiento de voz
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: - (bool, Valor que determina si se logró crear el motor de voz), motor de voz listo para funcionar
         */
        private bool CreateSpeechRecognizer()
        {
            //Se obtiene el reconocedor de voz de Kinect
            RecognizerInfo ri = GetKinectRecognizer();
            //Si el valor es nulo, no se logró encontrar, se informa y la función devuelve falso
            if (ri == null)
            {
                Console.WriteLine("No se encontró el reconocedor de voz de Kinect");
                return false;
            }
            //Se inicializa el motor
            speechRecognizer = new SpeechRecognitionEngine(ri.Id);
            //Se crea el constructor de gramática con la información de cultura del Kinect
            GrammarBuilder gb = new GrammarBuilder { Culture = ri.Culture };
            //Se obtiene la gramática desde nuestro objeto GrammeGenerator
            gb.Append(GrammarGenerator.GetGrammar(commandTree));
            //Se crea la gramática y se carga al motor de reconocimiento de voz
            Grammar g = new Grammar(gb);
            speechRecognizer.LoadGrammar(g);
            //Se establecen las funciones a llamarse cuando un comando de voz sea reconocido o detectado pero rechazado
            speechRecognizer.SpeechRecognized += SreSpeechRecognized;
            speechRecognizer.SpeechRecognitionRejected += SreSpeechRecognitionRejected;
            //Se devuelve verdadero
            return true;
        }

        /*
         * Función: IntializeSkeletonTracking
         * Descripción: Función que inicia el seguimiento de esqueletos
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: - Reconocimiento de esqueletos listo para funcionar
         */
        private void IntializeSkeletonTracking()
        {
            //Establecemos el modo de reconocimiento, lo habilitamos y señalamos la función que se encargara de manejar los frames
            sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(Sensor_SkeletonFrameReady);
        }

        /*
         * Función: InitializeKinectAndSpeech
         * Descripción: Función que inicializa el Kinect junto con su motor de voz
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: - (bool, Valor que determina si se logró la inicialización), Kinect y motor de voz listos para funcionar
         */
        private bool InitializeKinectAndSpeech()
        {
            //Se busca el primer sensor de Kinect que este conectado 
            sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            //Si no se logró encontrar un Kinect, se informa y la función devuelve falso
            if (sensor == null)
            {
                Console.WriteLine("No se encontró el sensor Kinect");
                return false;
            }
            //Se crear el motor de reconocimiento de voz, si no se logra se informa y la función devuelve falso 
            if (!CreateSpeechRecognizer())
            {
                Console.WriteLine("No se logró crear el reconocedor de voz");
                return false;
            }
            //Se inicia el sensor
            sensor.Start();
            //Se inicializa el reconocimiento de esqueletos
            IntializeSkeletonTracking();
            //Se establece el ángulo de fuente de voz en adaptativo
            sensor.AudioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            //Se inicia el micrófono del Kinect y se envía el audio al motor de reconocimiento de voz
            Stream kinectStream = sensor.AudioSource.Start();
            speechRecognizer.SetInputToAudioStream(kinectStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            //Se establecen los valores óptimos para reconocimiento de voz
            sensor.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
            //Se devuelve verdadero
            sensor.AudioSource.AutomaticGainControlEnabled = false;
            return true;
        }

        /*
         * Función: StopKinectAndSpeech
         * Descripción: Función que detiene el Kinect y el motor de reconocimiento de voz
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Kinect y motor de voz liberados
         */
        private void StopKinectAndSpeech()
        {
            //Se comprueba que exista un Kinect, de ser así se detiene el micrófono y el sensor y el Kinect se establece en null
            if (sensor != null)
            {
                sensor.AudioSource.Stop();
                sensor.Stop();
                sensor = null;
            }
            //Se comprueba que exista un motor de reconocimiento de voz, de ser así se eliminan sus funciones de llamada, se cancela el reconocimiento y se establece en null
            if (speechRecognizer != null)
            {
                speechRecognizer.SpeechRecognized -= SreSpeechRecognized;
                speechRecognizer.SpeechRecognitionRejected -= SreSpeechRecognitionRejected;
                speechRecognizer.RecognizeAsyncCancel();
                speechRecognizer = null;
            }
        }

        /*
         * Función: RejectSpeech
         * Descripción: Función que informa al usuario cuando una voz no se reconoce o es invalida
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Sonido
         */
        private void RejectSpeech()
        {
            //Si el programa estaba esperando un comando de voz, entonces emite un sonido que significa que el comando no se logró entender o era invalido.
            if (waiting)
            {
                using (SoundPlayer simpleSound = new SoundPlayer("repeat.wav"))
                {
                    simpleSound.Play();
                }
            }
        }

        /*
         * Función: SreSpeechRecognitionRejected
         * Descripción: Función que es llamada automáticamente cuando un comando de voz no es reconocido
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Sonido
         */
        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //Se llama a RejectSpeech para encargarse del sonido si es necesario.
            RejectSpeech();
        }

        /*
         * Función: SreSpeechRecognized
         * Descripción: Función que es llamada automáticamente cuando un comando de voz es reconocido
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Procesamiento del comando
         */
        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //Se comprueba que la confianza sea mayor al mínimo establecido
            if (e.Result.Confidence > CONFIDENCE)
            {
                //Se comprueba que la entrada de comandos no este bloqueada
                if (!blocked)
                {
                    //Si el comando es el inicial del árbol, entonces informamos al usuario que el programa está listo para recibir comandos de voz, establecemos la variable adecuada, enviamos un comando al programa en Python para solicitar el menú inicial en la página 1 y terminamos la función
                    if (e.Result.Text == commandTree.children.ElementAt(0).text && !waiting)
                    {
                        using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                        {
                            simpleSound.Play();
                        }
                        waiting = true;
                        sendBytes = Encoding.ASCII.GetBytes("menu " + commandTree.children.ElementAt(0).code + " " + menuPage.ToString());
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                    //Si el comando es de movimiento geométrico, se establecen las variables correspondientes y se emite el sonido correspondiente
                    else if (GrammarGenerator.GEOMETRIC_COMMANDS.Contains(e.Result.Text))
                    {
                        geometricWaiting = true;
                        geometricCommand = e.Result.Text;
                        using (SoundPlayer simpleSound = new SoundPlayer("tracking.wav"))
                        {
                            simpleSound.Play();
                        }
                    }
                    //Si el programa estaba esperando un comando de voz, se procesa el comando
                    else if (waiting)
                    {
                        ProcessAudioCommand(e.Result.Text);
                    }
                }
                //Se comprueba si el comando es el de desbloqueo, de ser así se desbloquea la entrada de comandos y se informa al usuario
                else if (e.Result.Text == GrammarGenerator.UNLOCK_COMMAND)
                {
                    blocked = false;
                    sendBytes = Encoding.ASCII.GetBytes("CONTINUE");
                    udpClient.Send(sendBytes, sendBytes.Length);
                    using (SoundPlayer simpleSound = new SoundPlayer("unlock.wav"))
                    {
                        simpleSound.Play();
                    }
                }
                //Si el usuario intenta usar otro comando, se le informa que actualmente está bloqueado el uso de comandos
                else if (e.Result.Text == commandTree.children.ElementAt(0).text || GrammarGenerator.GEOMETRIC_COMMANDS.Contains(e.Result.Text))
                {
                    using (SoundPlayer simpleSound = new SoundPlayer("locked.wav"))
                    {
                        simpleSound.Play();
                    }
                }
            }
            else
            {
                //La confianza no fue suficiente, por lo que se rechaza el comando de voz
                RejectSpeech();
            }
        }


        /*
         * Función: Sensor_SkeletonFrameReady
         * Descripción: Función que es llamada automáticamente cuando un frame de esqueletos es reconocido
         * Autor: Christian Vargas
         * Fecha de creación: 16/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Procesamiento del esqueleto
         */
        void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Se comprueba que se esté realizando un comando geométrico y la entrada de comandos no esta bloqueada
            if (geometricWaiting && !blocked)
            {
                //Se obtiene el frame de esqueletos
                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    //Se comprueba que el cuadro tenga información valida
                    if (skeletonFrame != null)
                    {
                        //Se crea un vector que guardara los datos de los esqueletos reconocidos
                        Skeleton[] skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        //Se copian los datos de los esqueletos
                        skeletonFrame.CopySkeletonDataTo(skeletonData);
                        //Se obtiene el primer esqueleto que este siendo rastreado
                        Skeleton playerSkeleton = (from s in skeletonData where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
                        //Se comprueba que el esqueleto tenga información valida
                        if (playerSkeleton != null)
                        {
                            //Se obtienen los datos de las posiciones corporales
                            Joint rightHand = playerSkeleton.Joints[JointType.HandRight];
                            Joint leftHand = playerSkeleton.Joints[JointType.HandLeft];
                            Joint rightShoulder = playerSkeleton.Joints[JointType.ShoulderRight];
                            Joint head = playerSkeleton.Joints[JointType.Head];
                            //Se inicializa una variable que se utilizara para enviar el comando a PyMOL
                            string message = "";
                            //Se inicializa una variable que indicara si el programa continuo ejecutándose
                            bool moving = true;
                            //Se codifica el comando geométrico al código comprendido por PyMOL
                            switch (geometricCommand)
                            {
                                //En caso de que el comando sea enfocar, obtenemos el valor de movimiento en un solo eje, enviamos el mensaje correspondiente al programa en PyMOL y si el usuario está terminando el movimiento, establecemos la variable correspondiente
                                case "enfocar":
                                    Tuple<bool, int> zoomValue = Gestures.Get1AxleValue(rightHand, leftHand, rightShoulder, head);
                                    if (zoomValue.Item1)
                                    {
                                        message = "move z, " + zoomValue.Item2;
                                        sendBytes = Encoding.ASCII.GetBytes(message);
                                        udpClient.Send(sendBytes, sendBytes.Length);
                                    }
                                    else
                                    {
                                        moving = false;
                                    }
                                    break;
                                //En caso de que el comando sea girar, obtenemos el valor de movimiento en un solo eje, enviamos el mensaje correspondiente al programa en PyMOL y si el usuario está terminando el movimiento, establecemos la variable correspondiente
                                case "girar":
                                    Tuple<bool, int> turnValue = Gestures.Get1AxleValue(rightHand, leftHand, rightShoulder, head);
                                    if (turnValue.Item1)
                                    {
                                        message = "turn z, " + -turnValue.Item2;
                                        sendBytes = Encoding.ASCII.GetBytes(message);
                                        udpClient.Send(sendBytes, sendBytes.Length);
                                    }
                                    else
                                    {
                                        moving = false;
                                    }
                                    break;
                                //En caso de que el comando sea mover, obtenemos el valor de movimiento en los dos ejes, enviamos el mensaje correspondiente al programa en PyMOL y si el usuario está terminando el movimiento, establecemos la variable correspondiente.
                                case "mover":
                                    Tuple<bool, int, int> moveValue = Gestures.Get2AxisValue(rightHand, leftHand, rightShoulder, head);
                                    if (moveValue.Item1)
                                    {
                                        message = "move x, " + -moveValue.Item2;
                                        sendBytes = Encoding.ASCII.GetBytes(message);
                                        udpClient.Send(sendBytes, sendBytes.Length);
                                        message = "move y, " + moveValue.Item3;
                                        sendBytes = Encoding.ASCII.GetBytes(message);
                                        udpClient.Send(sendBytes, sendBytes.Length);
                                    }
                                    else
                                    {
                                        moving = false;
                                    }
                                    break;
                                //En caso de que el comando sea rotar, obtenemos el valor de movimiento en los dos ejes, enviamos el mensaje correspondiente al programa en PyMOL y si el usuario está terminando el movimiento, establecemos la variable correspondiente.
                                case "rotar":
                                    Tuple<bool, int, int> rotationValue = Gestures.Get2AxisValue(rightHand, leftHand, rightShoulder, head);
                                    if (rotationValue.Item1)
                                    {
                                        message = "turn y, " + rotationValue.Item2;
                                        sendBytes = Encoding.ASCII.GetBytes(message);
                                        udpClient.Send(sendBytes, sendBytes.Length);
                                        message = "turn x, " + rotationValue.Item3;
                                        sendBytes = Encoding.ASCII.GetBytes(message);
                                        udpClient.Send(sendBytes, sendBytes.Length);
                                    }
                                    else
                                    {
                                        moving = false;
                                    }
                                    break;
                                //En cualquier otro caso, existió un error
                                default:
                                    Console.WriteLine("Comando geométrico no codificado, Sensor_SkeletonFrameReady");
                                    break;
                            }
                            //Si el movimiento ya termino, establecemos la variable correspondiente y emitimos un sonido para informar al usuario
                            if (!moving)
                            {
                                geometricWaiting = false;
                                geometricCommand = "";
                                using (SoundPlayer simpleSound = new SoundPlayer("understood.wav"))
                                {
                                    simpleSound.Play();
                                }
                            }
                        }
                    }
                }
            }
        }

        /*
         * Función: LoadDevices
         * Descripción: Función que carga el Kinect y el control de Xbox 360
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Dispositivos cargados y reconocidos
         */
        private void LoadDevices()
        {
            //Se carga el texto inicial de dispositivos detectados
            string newText = "Dispositivos detectados:\n";
            //Se establece el control de Xbox 360 en el primero conectado
            xboxControl = new Controller(UserIndex.One);
            //Se comprueba si está conectado, de ser así se añade en el texto y se establece la variable adecuada para reflejarlo
            if (xboxControl.IsConnected)
            {
                newText += "\n- Control Xbox 360";
                xboxControlDetected = true;
            }
            else
            {
                //Se refleja que el control no está conectado
                xboxControlDetected = false;
                Console.WriteLine("No se encontró el control de Xbox 360");
            }
            //Se inicializa el Kinect y el motor de audio, de lograrse, se añade en el texto de dispositivos conectados
            if (InitializeKinectAndSpeech())
            {
                newText += "\n- Kinect";
            }
            //Se actualiza el texto de dispositivos conectados
            label_Detected.Text = newText;
        }

        /*
         * Función: CheckDevices
         * Descripción: Función que comprueba los dispositivos conectados en el sistema
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Texto de dispositivos detectados actualizado
         */
        private void CheckDevices()
        {
            //Se carga el texto inicial de dispositivos detectados
            string newText = "Dispositivos detectados:\n";
            //Se comprueba si está conectado, de ser así se añade en el texto y se establece la variable adecuada para reflejarlo
            if (xboxControl.IsConnected)
            {
                newText += "\n- Control Xbox 360";
                xboxControlDetected = true;
            }
            else
            {
                //Se refleja que el control no está conectado
                xboxControlDetected = false;
            }
            //Se comprueba que el Kinect esté conectado, de ser así se añade en el texto de dispositivos conectados
            if (sensor != null && sensor.Status == KinectStatus.Connected)
            {
                newText += "\n- Kinect";
            }
            //Se actualiza el texto de dispositivos conectados
            label_Detected.Text = newText;
        }

        /*
         * Función: QuitMenu
         * Descripción: Función que envía al programa en Python la solicitud de eliminar el menú
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: requested (bool, variable que indica si la eliminación del menú fue por parte de un proceso normal)
         * Salidas: Menú eliminado en la pantalla del Oculus
         */
        private void QuitMenu(bool Requested)
        {
            //Comprobamos si la eliminación del menú fue requerida, de ser así el sonido correspondiente es emitido, caso contrario se rechaza el comando detectado
            if (Requested)
            {
                using (SoundPlayer simpleSound = new SoundPlayer("understood.wav"))
                {
                    simpleSound.Play();
                }
            }
            else
            {
                RejectSpeech();
            }
            //Se reinicializan las variables que participan en la generación de comandos y se envía el comando de eliminar el menú al programa en Python
            newCommand = "";
            molCode = "";
            selName = "";
            resISel = "";
            menuPage = 1;
            showingTitles = false;
            hearingMol = false;
            hearingSel = false;
            hearingResI = false;
            hearingFontSize = false;
            displayingRayWarning = false;
            displayingMolList = false;
            sendBytes = Encoding.ASCII.GetBytes("menu clear");
            udpClient.Send(sendBytes, sendBytes.Length);
            waiting = false;
        }

        /*
         * Función: GetPageCount
         * Descripción: Función que obtiene la cantidad de páginas en un menú
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: fullComand (string, variable que contiene el comando que lleva al menú)
         * Salidas: (int, valor que indica cuantas paginas tiene el menú, 0 si el comando fue inválido)
         */
        private int GetPageCount(string fullCommand)
        {
            //Si estamos mostrando una lista de moléculas, entonces devolvemos la cantidad de páginas en las que se mostraran
            if (displayingMolList)
            {
                return (int)Math.Ceiling(downloadedMol.Count() / (float)MAXLIST);
            }
            //Caso contrario, buscamos la cantidad de páginas para un menú del árbol de comandos
            else
            {
                //Se inicia una variable en 0 que permite saber cuántos subcomandos han sido reconocidos exitosamente
                int count = 0;
                //Se inicia el nodo actual en el menú raíz del árbol de comandos
                CommandNode actualNode = commandTree.children.ElementAt(0);
                //Se parte el comando completo en subcomandos que esta separados por espacios en blanco, por cada comando se busca encontrar su nodo correspondiente en el árbol de comandos
                foreach (string subCommand in fullCommand.Split(' '))
                {
                    foreach (CommandNode page in actualNode.children)
                    {
                        foreach (CommandNode son in page.children)
                        {
                            if (son.text == subCommand)
                            {
                                //Si el texto del hijo actual corresponde con el subcomando se establece el nodo actual en el hijo y se aumenta la cantidad de subcomandos reconocidos 
                                actualNode = son;
                                count++;
                            }
                        }
                    }
                }
                //Si la cantidad de subcomandos reconocidos es igual a la cantidad de subcomandos entonces el comando era válido, se regresa el total de páginas del menú, caso contrario se devuelve 0
                if (count == fullCommand.Split(' ').Count())
                {
                    return actualNode.children.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        /*
         * Función: GenCodeCommand
         * Descripción: Función que obtiene el código de un comando
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: fullComand (string, variable que contiene el comando)
         * Salidas: (string, código del comando, se devuelve una cadena vacía si el comando era inválido)
         */
        private string GenCodeCommand(string fullCommand)
        {
            //Se inicia una variable en 0 que permite saber cuántos subcomandos han sido reconocidos exitosamente
            int count = 0;
            //Variable que guarda el código generado
            string message = "";
            //Se inicia el nodo actual en el menú raíz del árbol de comandos
            CommandNode actualNode = commandTree.children.ElementAt(0);
            //Se parte el comando completo en subcomandos que esta separados por espacios en blanco, por cada comando se busca encontrar su nodo correspondiente en el árbol de comandos
            foreach (string subCommand in fullCommand.Split(' '))
            {
                foreach (CommandNode page in actualNode.children)
                {
                    foreach (CommandNode son in page.children)
                    {
                        //Si el texto del hijo actual corresponde con el subcomando se establece el nodo actual en el hijo, se aumenta el codigo y se aumenta la cantidad de subcomandos reconocidos 
                        if (son.text == subCommand)
                        {
                            if (message == "")
                            {
                                message = son.code;
                            }
                            else
                            {
                                message += " " + son.code;
                            }
                            actualNode = son;
                            count++;
                        }
                    }
                }
            }
            //Si la cantidad de subcomandos reconocidos es igual a la cantidad de subcomandos entonces el comando era válido, se regresa el código del comando, caso contrario se devuelve una cadena vacía
            if (count == fullCommand.Split(' ').Count())
            {
                return message;
            }
            else
            {
                return "";
            }
        }

        /*
         * Función: IsCommand
         * Descripción: Función que averigua si un comando corresponde a un comando final
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: fullComand (string, variable que contiene el comando)
         * Salidas: (bool, variable que indica si el comando es un comando final)
         */
        private bool IsCommand(string fullCommand)
        {
            //Se inicia el nodo actual en el menú raíz del árbol de comandos
            CommandNode actualNode = commandTree.children.ElementAt(0);
            //Se parte el comando completo en subcomandos que esta separados por espacios en blanco, por cada comando se busca encontrar su nodo correspondiente en el árbol de comandos
            foreach (string subCommand in fullCommand.Split(' '))
            {
                foreach (CommandNode page in actualNode.children)
                {
                    foreach (CommandNode son in page.children)
                    {
                        //Si el texto del hijo actual corresponde con el subcomando se establece el nodo actual en el hijo
                        if (son.text == subCommand)
                        {
                            actualNode = son;
                        }
                    }
                }
            }
            //Se devuelve la comprobación del tipo
            return actualNode.type == Types.Command;
        }

        /*
         * Función: InSpecialMenu
         * Descripción: Función que averigua si se está desplegando un menú especial
         * Autor: Christian Vargas
         * Fecha de creación: 23/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: (bool, variable que indica si se está desplegando un menú especial)
         */
        private bool InSpecialMenu()
        {
            return (showingTitles || hearingMol || hearingSel || hearingResI || hearingFontSize || displayingRayWarning || displayingMolList);
        }

        /*
         * Función: InListingMenu
         * Descripción: Función que averigua si se está desplegando una lista de objetos
         * Autor: Christian Vargas
         * Fecha de creación: 30/08/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: (bool, variable que indica si se está desplegando una lista de objetos)
         */
        private bool InListingMenu()
        {
            return displayingMolList;
        }

        /*
         * Función: HandleCommandByNumber
         * Descripción: Función que maneja los comandos del menú por numero
         * Autor: Christian Vargas
         * Fecha de creación: 22/08/15
         * Fecha de modificación: --/--/--
         * Entradas: fullCommand (string, variable que contiene el comando que hasta ahora se ha recibido), number (int, numero de opción dicho por le usuario)
         * Salidas: Menú actualizado en la pantalla del Oculus
         */
        private void HandleCommandByNumber(string fullCommand, int number)
        {
            //Se inicia una variable en 0 que permite saber cuántos subcomandos han sido reconocidos exitosamente
            int count = 0;
            //Se inicia el nodo actual en el menú raíz del árbol de comandos
            CommandNode actualNode = commandTree.children.ElementAt(0);
            //Se comprueba si el usuario aun no navega a un submenú, en cuyo caso se reconoce automáticamente un subcomando como valido
            if (fullCommand == "")
            {
                count++;
            }
            //Caso contrario, se busca el submenú actual
            else
            {
                //Se parte el comando completo en subcomandos que esta separados por espacios en blanco, por cada comando se busca encontrar su nodo correspondiente en el árbol de comandos
                foreach (string subCommand in fullCommand.Split(' '))
                {
                    foreach (CommandNode page in actualNode.children)
                    {
                        foreach (CommandNode son in page.children)
                        {
                            //Si el texto del hijo actual corresponde con el subcomando se establece el nodo actual en el hijo y se aumenta la cantidad de subcomandos reconocidos 
                            if (son.text == subCommand)
                            {
                                actualNode = son;
                                count++;
                            }
                        }
                    }
                }
            }
            //Si la cantidad de subcomandos reconocidos es igual a la cantidad de subcomandos entonces el comando era válido
            if (count == fullCommand.Split(' ').Count())
            {
                //Se comprueba que el numero este dentro del rango valido, de ser así se procesa el comando correspondiente.
                if (actualNode.children.ElementAt(menuPage - 1).children.Count >= number)
                {
                    ProcessAudioCommand(actualNode.children.ElementAt(menuPage - 1).children.ElementAt(number - 1).text);
                }
                //Caso contrario se rechaza el comando
                else
                {
                    RejectSpeech();
                }
            }
            //Caso contrario, el comando era invalido
            else
            {
                QuitMenu(false);
                Console.WriteLine("Un comando inválido no se detectó antes, invalidado en HandleCommandByNumber al generar el código del comando, comando:" + fullCommand);
                RejectSpeech();
            }
        }

        /*
         * Función: HandlePageCommands
         * Descripción: Función que maneja los comandos de control de paginas
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: command (string, variable que contiene el último comando recibido)
         * Salidas: Menú actualizado en la pantalla del Oculus
         */
        private void HandlePageCommands(string command)
        {
            //Variable que guarda el código generado
            string message;
            //Variable que guarda la cantidad de páginas en el menú actual
            int pageCount;
            //Se comprueba si el menú actual es el de la raíz, en caso de ser así se establece el comando y la cantidad de páginas correspondientes 
            if (newCommand == "")
            {
                message = commandTree.children.ElementAt(0).code;
                pageCount = commandTree.children.ElementAt(0).children.Count();
            }
            //Caso contrario los valores se obtienen buscando en el árbol usando las funciones correspondientes
            else
            {
                pageCount = GetPageCount(newCommand);
                message = GenCodeCommand(newCommand);
            }
            //Se comprueba que el comando haya sido valido, de no ser así se termina la ejecución de los menús.
            if (pageCount == 0)
            {
                QuitMenu(false);
                Console.WriteLine("Un comando inválido no se detectó antes, invalidado en HandlePageCommands al buscar total de páginas, comando: " + newCommand);
                return;
            }
            //Si el comando es siguiente se aumenta el número de página de forma circular, evitando un desbordamiento 
            if (command == "siguiente")
            {
                if (menuPage == pageCount)
                {
                    menuPage = 1;
                }
                else
                {
                    menuPage++;
                }
            }
            //Si el comando es anterior se disminuye el número de página de forma circular, evitando un desbordamiento 
            else if (command == "anterior")
            {
                if (menuPage == 1)
                {
                    menuPage = pageCount;
                }
                else
                {
                    menuPage--;
                }
            }
            //Se le agrega al código del comando el identificador de que es un menú y el número de página
            message = "menu " + message + " " + menuPage;
            //Se emite el sonido correspondiente a la espera de otro comando y se envía el mensaje
            using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
            {
                simpleSound.Play();
            }
            sendBytes = Encoding.ASCII.GetBytes(message);
            udpClient.Send(sendBytes, sendBytes.Length);
        }

        /*
         * Función: HandleMenuCommands
         * Descripción: Función que maneja los comandos de control de menús
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: command (string, variable que contiene el último comando recibido)
         * Salidas: Menú actualizado en la pantalla del Oculus
         */
        private void HandleMenuCommands(string command)
        {
            //Se comprueba si el menú es cancelar, de ser así se elimina el menú
            if (command == "cancelar")
            {
                QuitMenu(true);
            }
            //Se comprueban el resto de los comandos de menú
            else
            {
                //Si el menú es uno de los menús especiales sin posibilidad de páginas se rechaza el comando
                if (InSpecialMenu() && !InListingMenu())
                {
                    RejectSpeech();
                }
                //Caso contrario se maneja el comando con la función correspondiente
                else
                {
                    HandlePageCommands(command);
                }
            }
        }

        /*
         * Función: HandleDictationCommands
         * Descripción: Función que maneja los comandos de dictado
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: command (string, variable que contiene el último comando recibido)
         * Salidas: Menú actualizado en la pantalla del Oculus
         */
        private void HandleDictationCommands(string command)
        {
            //Variable que guarda el código generado
            string message;
            //Variable que respalda el código generado
            string tempname;
            //Se comprueba si el menú actual es el de la raíz, en caso de ser así se establece el comando correspondiente 
            if (newCommand == "")
            {
                message = commandTree.children.ElementAt(0).code;
            }
            //Caso contrario el valor se obtiene buscando en el árbol usando la función correspondiente
            else
            {
                message = GenCodeCommand(newCommand);
            }
            //Se comprueba que el comando haya sido valido, de no ser así se termina la ejecución de los menús
            if (message == "")
            {
                QuitMenu(false);
                Console.WriteLine("Un comando inválido no se detectó antes, invalidado en HandleDictationCommands al generar el código del comando, comando:" + newCommand);
                return;
            }
            //Se comprueba si el comando es el de borrar
            if (command == "borrar")
            {
                //Se comprueba si actualmente se está escuchando un código de molécula
                if (hearingMol)
                {
                    //Si el código actual está vacío se rechaza el comando
                    if (molCode == "")
                    {
                        RejectSpeech();
                    }
                    //De no ser así se remueve el último carácter del código de la molécula, se emite un sonido de espera de comando y se envía el comando al programa en Python
                    else
                    {
                        molCode = molCode.Remove(molCode.Length - 1);
                        message = "menu " + message + " " + molCode;
                        using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                        {
                            simpleSound.Play();
                        }
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                }
                //Se comprueba si actualmente se está escuchando un nombre de selección
                else if (hearingSel)
                {
                    //Si el nombre actual está vacío se rechaza el comando
                    if (selName == "")
                    {
                        RejectSpeech();
                    }
                    //De no ser así se remueve el último carácter del nombre de la selección, se emite un sonido de espera de comando y se envía el comando al programa en Python
                    else
                    {
                        selName = selName.Remove(selName.Length - 1);
                        message = "menu " + message + " " + selName;
                        using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                        {
                            simpleSound.Play();
                        }
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                }
                //Se comprueba si actualmente se están escuchando los enteros de una selección
                else if (hearingResI)
                {
                    //Si no hay enteros actualmente se rechaza el comando
                    if (resISel == "")
                    {
                        RejectSpeech();
                    }
                    else
                    {
                        //Si hay más de 1 entero actualmente entonces se elimina de la cadena todo a partir del último signo de +
                        if (resISel.Contains('+'))
                        {
                            resISel = resISel.Remove(resISel.LastIndexOf('+'));
                        }
                        //Caso contrario se limpia toda la cadena
                        else
                        {
                            resISel = "";
                        }
                        //Se emite un sonido de espera de comando y se envía el comando al programa en Python
                        message = "menu " + message + " " + selName + " HEAR_RESI " + resISel;
                        using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                        {
                            simpleSound.Play();
                        }
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                }
                //Se comprueba si actualmente se está escuchando el tamaño de una fuente
                else if (hearingFontSize)
                {
                    //Si no hay una fuente actualmente se rechaza el comando
                    if (fontSize == "")
                    {
                        RejectSpeech();
                    }
                    //Caso contrario se limpia toda la cadena, se emite un sonido de espera de comando y se envía el comando al programa en Python
                    else
                    {
                        fontSize = "";
                        message = "menu " + message + " " + fontSize;
                        using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                        {
                            simpleSound.Play();
                        }
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                }
                //Caso contrario se rechaza el comando
                else
                {
                    RejectSpeech();
                    return;
                }
            }
            //Se comprueba si el comando es el de aceptar
            else if (command == "aceptar")
            {
                //Si se están mostrando los títulos entonces se elimina el menú
                if (showingTitles)
                {
                    QuitMenu(true);
                }
                //Se comprueba si se está escuchando un código de molécula 
                else if (hearingMol)
                {
                    //Se comprueba si el código ya está completo, de ser así se forma el comando final, se elimina el menú y se envía el comando
                    //Si la respuesta de PyMOL indica que la molécula era válida, entonces esta se agrega a la lista de moléculas descargadas (la lista se ordena alfabéticamente)
                    if (molCode.Length == 4)
                    {
                        message = message.Replace("HEAR_MOL", molCode);
                        tempname = molCode;
                        QuitMenu(true);
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                        recvBytes = udpServer.Receive(ref endPoint);
                        if(Encoding.ASCII.GetString(recvBytes).ToString() == "200")
                        {
                            downloadedMol.Add(tempname);
                            downloadedMol.Sort();
                        }
                    }
                    //Caso contrario se rechaza el comando 
                    else
                    {
                        RejectSpeech();
                    }
                }
                //Se comprueba si actualmente se está escuchando un nombre de selección
                else if (hearingSel)
                {
                    /*
                     * Se comprueba que el nombre no este vacío, de ser así se forma el comando que indica que ahora escucharemos los enteros, se emite un sonido de espera de comando, 
                     * se establece que ya no se escucha el nombre de la selección y se envía el comando al programa en Python
                     */
                    if (selName != "")
                    {
                        if (hearingResI)
                        {
                            message = "menu " + message + " " + selName + " HEAR_RESI";
                        }
                        using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                        {
                            simpleSound.Play();
                        }
                        hearingSel = false;
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                    //Caso contrario se rechaza el comando 
                    else
                    {
                        RejectSpeech();
                    }
                }
                //Se comprueba si actualmente se están escuchando los enteros de una selección
                else if (hearingResI)
                {
                    //Se comprueba que si hayan números seleccionados, de ser así se forma el comando final, se elimina el menú y se envía el comando.
                    //Si la respuesta de PyMOL indica que la selección era válida, entonces esta se agrega a la lista de selecciones 
                    if (resISel != "")
                    {
                        message = message.Replace("IGNOREres", selName);
                        message = message.Replace("HEAR_SEL_RESI", resISel);
                        tempname = selName;
                        QuitMenu(true);
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                        recvBytes = udpServer.Receive(ref endPoint);
                        if (Encoding.ASCII.GetString(recvBytes).ToString() == "200")
                        {
                            selectedMol.Add(tempname);
                        }
                    }
                    //Caso contrario se rechaza el comando 
                    else
                    {
                        RejectSpeech();
                    }
                }
                //Se comprueba si actualmente se está escuchando el tamaño de una fuente
                else if (hearingFontSize)
                {
                    //Se comprueba que si exista un tamaño de fuente, de ser así se forma el comando final, se elimina el menú y se envía el comando.
                    if (fontSize != "")
                    {
                        message = message.Replace("IGNORElabel", "");
                        message = message.Replace("HEAR_FONT_SIZE", "," + fontSize);
                        QuitMenu(true);
                        sendBytes = Encoding.ASCII.GetBytes(message);
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                    //Caso contrario se rechaza el comando 
                    else
                    {
                        RejectSpeech();
                    }
                }
                //Se comprueba si actualmente se está desplegando una advertencia de bloqueo de comandos
                else if (displayingRayWarning)
                {
                    //Se forma el comando final, se elimina el menú y se envía el comando
                    message = message.Replace("PREPARE_RAY", "ray");
                    QuitMenu(true);
                    sendBytes = Encoding.ASCII.GetBytes(message);
                    udpClient.Send(sendBytes, sendBytes.Length);
                    //Se bloquea la entrada de comandos, ya que ray pierde su efecto si se recibe un comando
                    blocked = true;
                }
                //Caso contrario se rechaza el comando 
                else
                {
                    RejectSpeech();
                }
            }
        }

        /*
         * Función: HandleCharacterCommands
         * Descripción: Función que maneja los comandos de letras
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: command (string, variable que contiene el último comando recibido)
         * Salidas: Menú actualizado en la pantalla del Oculus
         */
        private void HandleCharacterCommands(string command)
        {
            //Variable que guarda el código generado
            string message;
            char character;  
            //Se comprueba si el menú actual es el de la raíz, en caso de ser así se establece el comando correspondiente 
            if (newCommand == "")
            {
                message = commandTree.children.ElementAt(0).code;
            }
            //Caso contrario el valor se obtiene buscando en el árbol usando la función correspondiente
            else
            {
                message = GenCodeCommand(newCommand);
            }
            //Se comprueba que el comando haya sido valido, de no ser así se termina la ejecución de los menús
            if (message == "")
            {
                QuitMenu(false);
                Console.WriteLine("Un comando inválido no se detectó antes, invalidado en HandleCharacterCommands al generar el código del comando, comando:" + newCommand);
                return;
            }
            //Se obtiene el valor del carácter proporcionado por el usuario (debido a que Kinect reconoce el carácter como se escucha, no como se escribe)
            character = GrammarGenerator.GetChar(command);
            //Se comprueba si se está escuchando un código molecular
            if (hearingMol)
            {
                //Se comprueba si aún faltan caracteres para completar el código molecular
                if (molCode.Count() < 4)
                {
                    //Se añade el carácter al final del código molecular, se emite un sonido de espera de comando y se envía el comando al programa en Python
                    molCode += character;
                    message = "menu " + message + " " + molCode;
                    using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                    {
                        simpleSound.Play();
                    }
                    sendBytes = Encoding.ASCII.GetBytes(message);
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                //Caso contrario se rechaza el comando 
                else
                {
                    RejectSpeech();
                }
            }
            //Se comprueba si actualmente se está escuchando un nombre de selección
            else if (hearingSel)
            {
                //Se añade el carácter al final del nombre de la selección, se emite un sonido de espera de comando y se envía el comando al programa en Python
                selName += character;
                message = "menu " + message + " " + selName;
                using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                {
                    simpleSound.Play();
                }
                sendBytes = Encoding.ASCII.GetBytes(message);
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            //Se comprueba si actualmente se están escuchando los enteros de una selección o el tamaño de una fuente
            else if (hearingResI || hearingFontSize)
            {
                //Se comprueba si el carácter es un número, de ser así se maneja el comando con el comando correspondiente
                if (char.IsNumber(character))
                {
                    HandleNumberCommands(command);
                }
                //Caso contrario se rechaza el comando 
                else
                {
                    RejectSpeech();
                }
            }
            //Se comprueba si actualmente se está mostrando una lista de moléculas
            else if (displayingMolList)
            {
                //Se comprueba si el número es válido, de ser así se forma el comando final, se elimina el menú y se envía el comando
                if (((menuPage - 1) * MAXLIST) + int.Parse(character.ToString()) <= downloadedMol.Count)
                {
                    message = message.Replace("IGNORErepresentation", "");
                    message = message.Replace("LIST_MOL", "(" + downloadedMol[((menuPage - 1) * MAXLIST) + int.Parse(character.ToString()) - 1] + ")");
                    QuitMenu(true);
                    sendBytes = Encoding.ASCII.GetBytes(message);
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                //Caso contrario se rechaza el comando 
                else
                {
                    RejectSpeech();
                }
            }
            //Se comprueba si el carácter es numérico, de ser así lo manejamos con la función correspondiente
            else if (char.IsNumber(character))
            {
                HandleCommandByNumber(newCommand, int.Parse(character.ToString()));
            }
            //Caso contrario se rechaza el comando 
            else
            {
                RejectSpeech();
            }
        }

        private void HandleNumberCommands(string command)
        {
            //Variable que guarda el código generado
            string message;
            string number;
            //Se comprueba si el menú actual es el de la raíz, en caso de ser así se establece el comando correspondiente 
            if (newCommand == "")
            {
                message = commandTree.children.ElementAt(0).code;
            }
            //Caso contrario el valor se obtiene buscando en el árbol usando la función correspondiente
            else
            {
                message = GenCodeCommand(newCommand);
            }
            //Se comprueba que el comando haya sido valido, de no ser así se termina la ejecución de los menús
            if (message == "")
            {
                QuitMenu(false);
                Console.WriteLine("Un comando inválido no se detectó antes, invalidado en HandleNumberCommands al generar el código del comando, comando:" + newCommand);
                return;
            }
            //Se obtiene el número del comando
            number = GrammarGenerator.GetNumber(command);
            //Se comprueba que el número sea válido, de no ser así se termina la ejecución de los menús
            if (number == "")
            {
                QuitMenu(false);
                Console.WriteLine("Un número inválido no se detectó antes, invalidado en HandleNumberCommands al obtener el número del sonido, comando:" + newCommand);
                return;
            }
            //Se comprueba si actualmente se están escuchando los enteros de una selección, se añade el número a la selección, se emite un sonido de espera de comando y se envía el comando al programa en Python
            if (hearingResI)
            {
                if (resISel == "")
                {
                    resISel = number;
                }
                else
                {
                    resISel += "+" + number;
                }
                message = "menu " + message + " " + selName + " HEAR_RESI " + resISel;
                using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                {
                    simpleSound.Play();
                }
                sendBytes = Encoding.ASCII.GetBytes(message);
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            //Se comprueba si actualmente se están escuchando el tamaño de una fuente
            else if (hearingFontSize)
            {
                //Si la fuente esta vacía, se añade el número a la fuente, se emite un sonido de espera de comando y se envía el comando al programa en Python
                if (fontSize == "")
                {
                    fontSize = number;
                    message = "menu " + message + " " + fontSize;
                    using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                    {
                        simpleSound.Play();
                    }
                    sendBytes = Encoding.ASCII.GetBytes(message);
                    udpClient.Send(sendBytes, sendBytes.Length);
                }
                //Caso contrario se rechaza el comando 
                else
                {
                    RejectSpeech();
                }
            }
            //Caso contrario se rechaza el comando 
            else
            {
                RejectSpeech();
            }
        }

        /*
         * Función: HandleNormalCommands
         * Descripción: Función que maneja los comandos normales (los que se encuentran en el árbol)
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: command (string, variable que contiene el último comando recibido)
         * Salidas: Menú actualizado en la pantalla del Oculus o ejecución de un comando en PyMOL
         */
        private void HandleNormalCommands(string command)
        {
            //Variable que guarda el código generado
            string message;
            //Variable que guarda el nuevo comando
            string tempCommand;
            //Variable utilizada para limpiar el código generado
            string tempMessage = "";
            //Se concatena el comando actual con los comandos anteriores
            if (newCommand == "")
            {
                tempCommand = command;
            }
            else
            {
                tempCommand = newCommand + " " + command;
            }
            //Se obtiene el código del comando
            message = GenCodeCommand(tempCommand);
            //Si el comando es invalido, entonces se rechaza y termina la función 
            if (message == "")
            {
                RejectSpeech();
                return;
            }
            //Se comprueba si el comando requiere que se muestren los títulos de las moléculas, de ser así se establece la variable correspondiente
            if (message.Contains("SHOW_NAME"))

            {
                showingTitles = true;
            }
            //Se comprueba si el comando requiere que se escuche el código de una molécula, de ser así se establece la variable correspondiente
            else if (message.Contains("HEAR_MOL"))
            {
                hearingMol = true;
            }
            //Se comprueba si el comando requiere que se escuche el nombre de una selección y sus enteros, de ser así se establecen las variables correspondientes
            else if (message.Contains("HEAR_SEL_RESI"))
            {
                hearingResI = true;
                hearingSel = true;
            }
            //Se comprueba si el comando requiere que se escuche el tamaño de una fuente, de ser así se establecen las variables correspondientes
            else if (message.Contains("HEAR_FONT_SIZE"))
            {
                hearingFontSize = true;
            }
            //Se comprueba si el comando requiere advertir al usuario sobre la renderización, de ser así se establecen las variables correspondientes
            else if (message.Contains("PREPARE_RAY"))
            {
                displayingRayWarning = true;
            }
            //Se comprueba si el comando requiere mostrar la lista de moléculas, de ser así se establecen las variables correspondientes
            else if (message.Contains("LIST_MOL"))
            {
                displayingMolList = true;
            }
            //Se comprueba si el comando requiere que se finalice la ejecución del el programa, de ser así se para el reconocimiento de voz, la actualización del control y se permite la entrada de datos en la ventana del programa
            else if (message.Contains("QUIT"))
            {
                timer_ControlPyMOL.Enabled = false;
                speechRecognizer.RecognizeAsyncCancel();
                EnableWindowInput();
            }
            //Se comprueba si el comando está listo para ser procesado por PyMOL, de ser así se limpia el comando y se elimina el menú
            if (IsCommand(tempCommand) && (!InSpecialMenu()))
            {
                foreach (string subMessage in message.Split(' '))
                {
                    if (!subMessage.StartsWith("IGNORE"))
                    {
                        if (tempMessage == "")
                        {
                            tempMessage = subMessage;
                        }
                        else
                        {
                            tempMessage += " " + subMessage;
                        }
                    }
                }
                if (tempMessage.Contains("DEL"))
                {
                    tempMessage = tempMessage.Substring(tempMessage.LastIndexOf("DEL") + 3);
                }
                message = tempMessage;
                QuitMenu(true);
            }
            //Caso contrario se prepara el comando de menú
            else
            {
                //Si estamos en un menú sin páginas entonces solo se antepone la palabra menú en el comando, caso contrario también se envía el número de página (establecido en 1)
                if (InSpecialMenu() && !InListingMenu())
                {
                    message = "menu " + message;
                }
                else
                {
                    menuPage = 1;
                    message = "menu " + message + " " + menuPage;
                }
                //Se actualiza el comando acumulado
                if (newCommand == "")
                {
                    newCommand = command;
                }
                else
                {
                    newCommand += " " + command;
                }
                //Se emite un sonido de espera de comando
                using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                {
                    simpleSound.Play();
                }
            }
            //Se envía el comando al programa en Python
            sendBytes = Encoding.ASCII.GetBytes(message);
            udpClient.Send(sendBytes, sendBytes.Length);
        }

        /*
         * Función: ProcessAudioCommand
         * Descripción: Función que procesa los comandos de voz
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: command (string, variable que contiene el último comando recibido)
         * Salidas: Menú actualizado en la pantalla del Oculus o ejecución de un comando en PyMOL
         */
        private void ProcessAudioCommand(string command)
        {
            //Se comprueba si el comando es de menú y se maneja con la función correspondiente
            if (GrammarGenerator.MENU_COMMANDS.Contains(command))
            {
                HandleMenuCommands(command);
            }
            //Se comprueba si el comando es de dictado y se maneja con la función correspondiente
            else if (GrammarGenerator.DICTATION_COMMANDS.Contains(command))
            {
                HandleDictationCommands(command);
            }
            //Se comprueba si el comando es de carácter y se maneja con la función correspondiente
            else if (GrammarGenerator.CHARACTERS_SOUNDS.Contains(command) || GrammarGenerator.ALT_CHARACTERS_SOUNDS.Contains(command))
            {
                HandleCharacterCommands(command);
            }
            else if (GrammarGenerator.numbers_sounds.Contains(command))
            {
                HandleNumberCommands(command);
            }
            //El comando es normal y se maneja con la función correspondiente
            else
            {
                HandleNormalCommands(command);
            }
        }

        /*
         * Función: DisableWindowInput
         * Descripción: Función que deshabilita la entrada en la ventana
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Entrada de la ventana deshabilitada
         */
        private void DisableWindowInput()
        {
            button_LoadDevices.Enabled = false;
            button_Launch.Enabled = false;
        }

        /*
         * Función: EnableWindowInput
         * Descripción: Función que habilita la entrada en la ventana
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Entrada de la ventana habilitada
         */
        private void EnableWindowInput()
        {
            button_LoadDevices.Enabled = true;
            button_Launch.Enabled = true;
        }

        /*
         * Función: XboxControlUpdate
         * Descripción: Función que actualiza los comandos desde el control de Xbox 360
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: Control de Xbox 360
         * Salidas: (bool, valor que determina si el  usuario solicito terminar la ejecución de PyMOL), comandos por UDP enviados hacia PyMOL
         */
        private bool XboxControlUpdate()
        {
            //Comprobamos que el control siga conectado, de no ser así actualizamos los dispositivos detectados y devolvemos verdadero
            if (!xboxControl.IsConnected)
            {
                CheckDevices();
                return true;
            }
            //Obtenemos el estado del control actual
            State controlState = xboxControl.GetState();
            //Se comprueba si el botón back está presionado, de ser así se envía un mensaje de terminación y se devuelve falso
            if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Back))
            {
                sendBytes = Encoding.ASCII.GetBytes("QUIT");
                udpClient.Send(sendBytes, sendBytes.Length);
                return false;
            }
            //Comprobamos que se estén esperando comandos
            if (waiting)
            {
                //Si el usuario presiona el botón A, lo manejamos como si dijera aceptar
                if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                {
                    HandleDictationCommands("aceptar");
                    //Se hace una pausa para no detectar más de una vez el botón
                    Thread.Sleep(SLEEPTIME);
                }
                //Si el usuario presiona el botón B, lo manejamos como si dijera borrar
                else if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
                {
                    HandleDictationCommands("borrar");
                    //Se hace una pausa para no detectar más de una vez el botón
                    Thread.Sleep(SLEEPTIME);
                }
                //Si el usuario presiona el botón Y, lo manejamos como si dijera cancelar
                else if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Y))
                {
                    HandleMenuCommands("cancelar");
                    //Se hace una pausa para no detectar más de una vez el botón
                    Thread.Sleep(SLEEPTIME);
                }
                //Si el usuario presiona el botón derecha, lo manejamos como si dijera siguiente
                else if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
                {
                    HandleMenuCommands("siguiente");
                    //Se hace una pausa para no detectar más de una vez el botón
                    Thread.Sleep(SLEEPTIME);
                }
                //Si el usuario presiona el botón izquierda, lo manejamos como si dijera anterior
                else if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
                {
                    HandleMenuCommands("anterior");
                    //Se hace una pausa para no detectar más de una vez el botón
                    Thread.Sleep(SLEEPTIME);
                }
            }
            //Si no se están esperando comandos...
            else
            {
                //Informamos al usuario que el programa está listo para recibir comandos de voz, establecemos la variable adecuada y enviamos un comando al programa en Python para solicitar el menú inicial en la página 1
                if (controlState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start))
                {
                    using (SoundPlayer simpleSound = new SoundPlayer("ready.wav"))
                    {
                        simpleSound.Play();
                    }
                    waiting = true;
                    sendBytes = Encoding.ASCII.GetBytes("menu " + commandTree.children.ElementAt(0).code + " " + menuPage.ToString());
                    udpClient.Send(sendBytes, sendBytes.Length);
                    //Se hace una pausa para no detectar más de una vez el botón
                    Thread.Sleep(SLEEPTIME);
                }
            }
            /*
             * Las siguientes condiciones comprueban que las palancas se encuentren fuera de la zona muerta del control
             * Posteriormente verifican si la palanca esta leve o fuertemente presionada, 
             * dependiendo de esto se envía un mensaje de rotación con intensidades proporcionales a la fuerza ejercida sobre las palancas
             */
            //Palanca izquierda, arriba y abajo
            if (Math.Abs((long)controlState.Gamepad.LeftThumbY) >= XBOXCONTROLDEADZONE)
            {
                if (Math.Abs((long)controlState.Gamepad.LeftThumbY) >= MAXTHUMBVAL / 2)
                {
                    sendBytes = Encoding.ASCII.GetBytes("turn x, " + (2 * -xboxControlSensitivity * Math.Sign(controlState.Gamepad.LeftThumbY)).ToString());
                }
                else
                {
                    sendBytes = Encoding.ASCII.GetBytes("turn x, " + (-xboxControlSensitivity * Math.Sign(controlState.Gamepad.LeftThumbY)).ToString());
                }
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            //Palanca izquierda, derecha e izquierda
            if (Math.Abs((long)controlState.Gamepad.LeftThumbX) >= XBOXCONTROLDEADZONE)
            {
                if (Math.Abs((long)controlState.Gamepad.LeftThumbX) >= MAXTHUMBVAL / 2)
                {
                    sendBytes = Encoding.ASCII.GetBytes("turn y, " + (2 * xboxControlSensitivity * Math.Sign(controlState.Gamepad.LeftThumbX)).ToString());
                }
                else
                {
                    sendBytes = Encoding.ASCII.GetBytes("turn y, " + (xboxControlSensitivity * Math.Sign(controlState.Gamepad.LeftThumbX)).ToString());
                }
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            //Palanca derecha, arriba y abajo
            if (Math.Abs((long)controlState.Gamepad.RightThumbY) >= XBOXCONTROLDEADZONE)
            {
                if (Math.Abs((long)controlState.Gamepad.RightThumbY) >= MAXTHUMBVAL / 2)
                {
                    sendBytes = Encoding.ASCII.GetBytes("move z, " + (2 * xboxControlSensitivity * Math.Sign(controlState.Gamepad.RightThumbY)).ToString());
                }
                else
                {
                    sendBytes = Encoding.ASCII.GetBytes("move z, " + (xboxControlSensitivity * Math.Sign(controlState.Gamepad.RightThumbY)).ToString());
                }
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            //Palanca derecha, derecha e izquierda
            if (Math.Abs((long)controlState.Gamepad.RightThumbX) >= XBOXCONTROLDEADZONE)
            {
                if (Math.Abs((long)controlState.Gamepad.RightThumbX) >= MAXTHUMBVAL / 2)
                {
                    sendBytes = Encoding.ASCII.GetBytes("turn z, " + (2 * -xboxControlSensitivity * Math.Sign(controlState.Gamepad.RightThumbX)).ToString());
                }
                else
                {
                    sendBytes = Encoding.ASCII.GetBytes("turn z, " + (-xboxControlSensitivity * Math.Sign(controlState.Gamepad.RightThumbX)).ToString());
                }
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            //Se devuelve verdadero
            return true;
        }

        /*
         * Función: Form_Main_Load
         * Descripción: Función que es llamada cuando la aplicación inicia
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Programa inicializado
         */
        private void Form_Main_Load(object sender, EventArgs e)
        {
            //Se genera el árbol de comandos
            commandTree = new CommandNode(Types.Root, "", "");
            commandTree.CreateTree("commands.xml");
            //Se inicializan las listas
            downloadedMol = new List<string>();
            selectedMol = new List<string>();
            //Se cargan los dispositivos y se inicializan los clientes y servidores UDP
            LoadDevices();
            udpClient = new UdpClient(IP, OUT_PORT);
            endPoint = new IPEndPoint(IPAddress.Parse(IP), IN_PORT);
            udpServer = new UdpClient(endPoint);
        }

        /*
         * Función: button_LoadDevices_Click
         * Descripción: Función que es llamada cuando se presiona el botón de cargar dispositivos
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Dispositivos cargados
         */
        private void button_LoadDevices_Click(object sender, EventArgs e)
        {
            //Se cargan los dispositivos
            LoadDevices();
        }

        /*
         * Función: button_Launch_Click
         * Descripción: Función que es llamada cuando se presiona el botón de lanzar
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: PyMOL y los controles por dispositivos inician
         */
        private void button_Launch_Click(object sender, EventArgs e)
        {
            /*
             * Comprobamos que el Kinect esté listo, de ser así se deshabilita la entrada en la ventana, se inicia el programa con PyMOL, 
             * se inicia el temporizador de controles y se empieza a recibir comandos de voz de forma asíncrona
             */
            if (sensor != null && sensor.Status == KinectStatus.Connected)
            {
                DisableWindowInput();
                Process.Start("Oculus.py", "1");
                timer_ControlPyMOL.Enabled = true;
                speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {
                //Se le informa al usuario que Kinect es obligatorio y se comprueban los dispositivos conectados
                MessageBox.Show("El sensor Kinect debe estar conectado", "¡Advertencia!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                CheckDevices();
            }
        }

        /*
         * Función: timer_ControlPyMOL_Tick
         * Descripción: Temporizador que comprueba el estado de los controles por dispositivos
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: Control de Xbox 360
         * Salidas: PyMOL actualizado
         */
        private void timer_ControlPyMOL_Tick(object sender, EventArgs e)
        {
            //Si el control de Xbox fue detectado y la entrada de comandos no esta bloqueada, se obtiene una actualización de él
            if (xboxControlDetected && !blocked)
            {
                /*
                 * Si el control informa que el usuario decidió terminar la ejecución de PyMOL, se deshabilita el temporizador, 
                 * se cancela el reconocimiento de voz asíncrono y se habilita la entrada en la ventana
                 */
                if (!XboxControlUpdate())
                {
                    timer_ControlPyMOL.Enabled = false;
                    speechRecognizer.RecognizeAsyncCancel();
                    EnableWindowInput();
                }
            }
        }

        /*
         * Función: Form_Main_FormClosing
         * Descripción: Función que es llamada cuando e programa está siendo cerrado
         * Autor: Christian Vargas
         * Fecha de creación: 07/06/15
         * Fecha de modificación: --/--/--
         * Entradas: --
         * Salidas: Recursos liberados
         */
        private void Form_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Se envía un mensaje a PyMOL para que cierre y se detiene el Kinect
            sendBytes = Encoding.ASCII.GetBytes("QUIT");
            udpClient.Send(sendBytes, sendBytes.Length);
            StopKinectAndSpeech();
        }
    }
}
