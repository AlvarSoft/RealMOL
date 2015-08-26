# -*- coding: utf-8 -*-
"""
Created on Sat May 30 15:47:46 2015

@author: Christian010
"""

from win32api import GetSystemMetrics #Utilizada para obtener la resolución de la pantalla
from win32api import keybd_event #Utilizada para enviar teclas al sistema

pymol_argv = ['pymol', '-xq'] #Argumentos leídos por PyMOL al ser cargado, x permite iniciar sin la interfaz de ventanas y q suprime los mensajes de bienvenida
import pymol #Utilizado para visualizar moléculas
import pymol.cgo
from pymol.vfont import plain

import ovrsdk #Utilizado para acceder a los datos de posición del Oculus
import socket #Utilizado para recibir los comandos por parte del Kinect

import re

UDP_IP = "127.0.0.1" #Dirección IP local
IN_PORT = 5005 #Puerto por donde llegaran los comandos del Kinect
OUT_PORT = 5006 #Puerto por donde saldrám los comandos a Kinect

XMLFILE = "commands.xml" #Archivo con los comandos

MAXLINEREAD = 10 #Cantidad de líneas a ser leídas para buscar los títulos de las moléculas
MAXLINESIZE = 55 #Tamaño máximo para una línea a ser impresa en pantalla
LINESEPARATION = 3 #Separación que existe entre línea y línea al ser impresas pantalla

POSITION = [0, 500, 0]
AXES = [[2.0,0.0,0.0],[0.0,2.0,0.0],[0.0,0.0,2.0]]
TEXTNAME = "reserved_text"

import time #Utilizado para dormir el programa durante un determinado tiempo y ahorrar CPU

SLEEPTIME = 0.0005 #Tiempo durante el cual duerme el programa en el ciclo principal

import sys #Utilizado para obtener los argumentos de inicio del programa
import os #Utilizado para comprobar la existencia de archivos

from enum import Enum #Utilizado para crear los tipos de nodo en el árbol de comandos
from xml.etree import ElementTree #Utilizado para leer archivos XML

class Types(Enum):
    Root = 1
    Menu = 2
    Page = 3
    Command = 4

class CommandNode:
    
    """
    Función: init
    Descripción: Constructor de un nodo de árbol de comando
    Autor: Christian Vargas
    Fecha de creación: 30/07/15
    Fecha de modificación: --/--/--
    Entradas: nodeType (Types, el tipo de nodo), nodeText (string, el texto del nodo), nodeCode (string, el código del nodo)
    Salidas: (CommandNode, el nodo de árbol de comando con datos validos)
    """
    def __init__(self, nodeType, nodeText, nodeCode):
        self.type = nodeType
        self.text = nodeText
        self.code = nodeCode
        self.children = []
        
    """
    Función: add_child
    Descripción: Función que añade un hijo a un nodo de árbol de comandos
    Autor: Christian Vargas
    Fecha de creación: 30/07/15
    Fecha de modificación: --/--/--
    Entradas: childType (Types, el tipo de nodo), childText (string, el texto del nodo), childCode (string, el código del nodo)
    Salidas: (CommandNode, el hijo del nodo de árbol de comando con datos validos)
    """
    def add_child(self, childType, childText, childCode):
        child = CommandNode(childType, childText, childCode)
        self.children.append(child)
        return child
    
    """
    Función: add_tree_childs
    Descripción: Función recursiva que recorre el archivo XML y llena el árbol de comandos
    Autor: Christian Vargas
    Fecha de creación: 30/07/15
    Fecha de modificación: --/--/--
    Entradas: Nodo inicial
    Salidas: Árbol cargado con datos del XML
    """    
    def add_tree_childs(self, parent, xmlNode):
        if xmlNode.tag == "menu":
            newNode =  parent.add_child(Types.Menu, xmlNode.getchildren()[0].text, xmlNode.getchildren()[1].text)
            for i in range (2, len(xmlNode.getchildren())):
                self.add_tree_childs(newNode, xmlNode.getchildren()[i])
        elif xmlNode.tag == "page":
            newNode =  parent.add_child(Types.Page, "", "")
            for i in range (len(xmlNode.getchildren())):
                self.add_tree_childs(newNode, xmlNode.getchildren()[i])
        elif xmlNode.tag == "command":
            newNode = parent.add_child(Types.Command, xmlNode.getchildren()[0].text, xmlNode.getchildren()[1].text)
   
    """
    Función: CreateTree
    Descripción: Función recursiva que recorre el archivo XML y llena el árbol de comandos
    Autor: Christian Vargas
    Fecha de creación: 30/07/15
    Fecha de modificación: --/--/--
    Entradas: Nodo inicial
    Salidas: Árbol cargado con datos del XML
    """    
    def create_tree(self, fileName):
        document = ElementTree.parse(fileName)
        root = document.getroot()
        self.add_tree_childs(self, root)

class RealMOL:
    
    def __init__(self,debug=False):
        #Se crea el árbol de comandos y se carga con los datos del XML
        self.commandTree = CommandNode(Types.Root, "", "")
        self.commandTree.create_tree(XMLFILE)
        self.debug = debug;
        #Valor de sensibilidad para el movimiento del OVR
        self.ovr_sensitivity = 80
        #Variable que controla la ejecución y finalización del programa
        self.running = True
        #Variable que indica si se está bloqueando el uso del movimiento
        self.moveBlocked = False

        #Si no estamos depurando, inicializamos el OVR
        if not self.debug:
            #Se inicializa el OVR
            self.hmd = self.__initialize_ovr()
            #Valores de posición anteriores del OVR, inicializados en 0
            self.prevp = [0, 0]
        #Socket que se utilizara para escuchar los comandos del Kinect
        self.sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
        self.sock.bind((UDP_IP, IN_PORT))
        #Establecemos el socket como no bloqueante para poder recibir comandos sin detener el rastreo del Oculus
        self.sock.setblocking(0)
        #Buffer en donde se guardaran los comandos del Kinect
        self.data = ""

        #Se inicializa PyMOL
        self.__initialize_pymol()

        #Lista de moléculas cargadas con su titulo
        self.mollist = {}

        #Variable que guarda la posición de la cámara antes de desplegar un menú
        self.backupView = ()    

    """
    Función: rotate_screen
    Descripción: Función que cambia la orientación de la pantalla aprovechando combinaciones de teclas
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: orientation (str, dirección hacia la cual se rotara la pantalla)
    Salidas: El sistema poseerá una nueva orientación de pantalla
    Notas: Solo funciona en computadoras con adaptadores de pantalla Intel
    """
    def __rotate_screen(self,orientation):
        #Se crea un diccionario que mapea los comandos de rotación con los valores de las teclas de dirección (izquierda, arriba, derecha, abajo)
        vals = dict(zip(['left', 'up', 'right', 'down'],
                        [37, 38, 39, 40]))
        #Se comprueba que el comando de entrada sea válido buscándolo en el diccionario de comandos, si no se encuentra la función termina (con mensaje informativo)
        if orientation not in vals:
            print ("Orientación invalida, la pantalla no será rotada")
            return
        #Se crea una tupla con la combinación de teclas para rotar la pantalla, la primera es la tecla Alt Gr y la segunda la flecha de dirección correspondiente a orientation
        comb = 165, vals[orientation]
        #Se envía la combinación de teclas presionando en el orden normal de la tupla y soltando en el orden inverso
        for k in comb:
            keybd_event(k, 0, 1, 0)
        for k in reversed(comb):
            keybd_event(k, 0, 2, 0)

    """
    Función: initialize_ovr
    Descripción: Función que inicializa el OVR para poder obtener los datos de orientación del aparato
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: --
    Salidas: hmd (class, objeto que nos permite interactuar fácilmente con el OVR)
    """
    def __initialize_ovr(self):
        """
        Dentro de un bloque try se inicializa el OVR, si este bloque no consigue ejecutarse se informa el error
        y el programa termina, en caso de éxito se imprime el nombre del OVR y se devuelve el objeto que nos permite
        interactuar con el OVR
        """
        try:
            ovrsdk.ovr_Initialize()
            hmd = ovrsdk.ovrHmd_Create(0)
            hmdDesc = ovrsdk.ovrHmdDesc()
            ovrsdk.ovrHmd_GetDesc(hmd, ovrsdk.byref(hmdDesc))
            print ("OVR inicializado correctamente, versión: " + hmdDesc.ProductName)
            ovrsdk.ovrHmd_StartSensor( \
                hmd,
                ovrsdk.ovrSensorCap_Orientation |
                ovrsdk.ovrSensorCap_YawCorrection,
                0
            )
            return hmd
        except:
            print("Error al inicializar el OVR")
            exit(1)

    """
    Función: initialize_pymol
    Descripción: Función que inicializa PyMOL para empezar a mostrar visión estéreo en el Oculus
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: debug (bool, indica a la función si va a trabajar en el Oculus o en pantalla con motivos de depuración)
    Salidas: PyMOL listo para trabajar con visión estéreo
    """
    def __initialize_pymol(self):
        #Si no estamos depurando, hacemos comprobaciones y ajustes de resolución y orientación de pantalla
        if not self.debug:
            #Se comprueba que la resolución actual sea 1920 X 1080 en caso contrario se imprime un mensaje y el programa termina (la visualización no funciona bien si el Oculus no trabaja a esta resolución)
            if GetSystemMetrics(0) != 1920 or GetSystemMetrics(1) != 1080:
                print("La resolución actual no es compatible, debe trabajarse con 1920 X 1080")
                exit(1)
            #Se rota la pantalla a la izquierda (el Oculus trabaja en una orientación portrait)
            #rotate_screen('left')
        #Se inicializa PyMOL
        pymol.finish_launching()
        #Se activa el modo estéreo con wall-eye
        pymol.cmd.set('stereo_mode',3)
        pymol.cmd.stereo()
        #Se desactiva la interfaz interna para tener una vista limpia en el Oculus
        pymol.cmd.set('internal_gui',0)
        #Si no estamos depurando, lanzamos PyMOL a máxima resolución en pantalla completa
        if not self.debug:
            pymol.cmd.viewport(1920,1080)
            pymol.cmd.full_screen('on')
        
    """
    Función: print_text
    Descripción: Función que imprime un mensaje de texto en la pantalla de PyMOL
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: text (str, texto a imprimir)
    Salidas: Mensaje en pantalla 
    """
    def __print_text(self,text):
        #Para evitar que el texto se imprima en un angulo difícil de leer reseteamos la cámara de PyMOL
        pymol.cmd.reset()
        #Creamos nuestro CGO
        cgo = []
        #Variable que nos ira dando la separación entre líneas
        separation = 0
        #Cambiamos la ñ por la n para poder imprimir el caracter
        cleanText = ""
        for char in text:
            if unicode(char) == u'ñ':
                cleanText += 'n'
            else:
                cleanText += char
        #Dividimos el texto en líneas
        for line in cleanText.split('\n'):
            #Hacemos un ciclo que ira en intervalos del tamaño máximo de línea hasta consumir toda la linea 
            for i in range (0, len(line), MAXLINESIZE):
                #Obtenemos la posición en donde se imprimirá el texto
                newPosition = list(POSITION)
                #Ajustamos la altura a la cual se imprimirá el texto de acuerdo a la separación entre líneas
                newPosition[1] -= separation
                #Calculamos el fin del texto a imprimir, teniendo cuidado de no desbordar la línea original
                if (i+MAXLINESIZE < len(line)):
                    end = i+MAXLINESIZE
                else:
                    end = len(line)
                #Obtenemos la línea a imprimir y la guardamos en el CGO
                printableLine = line[i:end]
                pymol.cgo.cyl_text(cgo, plain, newPosition, printableLine, 0.10, axes=AXES)
                #Aumentamos la separación
                separation += LINESEPARATION
        #Cargamos el CGO y acercamos la cámara a el
        pymol.cmd.load_cgo(cgo, TEXTNAME)
        pymol.cmd.zoom(TEXTNAME, 2.0)

    """
    Función: menu_titles
    Descripción: Función que imprime la lista de moléculas cargadas junto con su descripcion
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: mollist (dict, lista de moléculas junto con su descripcion)
    Salidas: Mensaje en pantalla 
    """
    def __menu_titles(self):
        #Variable que guarda el texto del menú
        menuText = ""
        #Se comprueba si no hay moléculas cargadas, de ser así se le informa al usuario en el texto del menú
        if len(self.mollist.items()) == 0:
            menuText += "No hay moleculas\n"
            menuText += "\n \n   Aceptar"
        #Caso contrario, se guarda el nombre de la molécula junto con su descripción en el texto del menú    
        else:
            for k, v in self.mollist.items():
                menuText += k + ".- " + v + "\n"
            menuText += "\n \n                       Aceptar"
        #Se imprime el menú
        self.__print_text(menuText)
    
    """
    Función: menu_mcode
    Descripción: Función que imprime la pantalla de dictado de moléculas
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: code (str, el código que hasta ahora se ha dictado)
    Salidas: Mensaje en pantalla 
    """
    def __menu_mcode(self, code):
        #Se carga el menú con su texto inicial
        menuText = "Dicte el codigo de la molecula\n \n"
        #Se comprueba que el código no este vacío, entonces se imprime el código
        if code != "HEAR_MOL":
            menuText += "          " + code
        else:
            menuText += "\n "
        #Si el código ya está completo se imprimen las opciones de comandos de voz junto con la de aceptar
        if len(code) == 4:
            menuText += "\n \nBorrar  Cancelar  Aceptar"
        #Caso contrario se imprimen solo las opciones básicas
        else:
            menuText += "\n \nBorrar  Cancelar"
        #Se imprime el menú
        self.__print_text(menuText)

    """
    Función: menu_hresi
    Descripción: Función que imprime la pantalla de dictado de los números de los residuos
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: res (str, los números que hasta ahora se han dictado)
    Salidas: Mensaje en pantalla 
    """
    def __menu_hresi(self, res):
        #Se carga el menú con su texto inicial
        menuText = "Dicte los numeros de los residuos\n \n"
        #Se comprueba que se haya dictado al menos un número 
        if not res.startswith("HEAR_RESI"):
            #Se imprimen los números
            menuText += res
            #Si los números no están vacíos entonces se imprime la opción de aceptar
            if len(res) > 0:
                menuText += "\n \nBorrar  Cancelar  Aceptar"
            #Caso contrario se imprimen solo las opciones básicas        
            else:
                menuText += "\n \nBorrar  Cancelar"
        #Caso contrario se imprimen solo las opciones básicas
        else:
            menuText += "\n \n \nBorrar  Cancelar"
        #Se imprime el menú
        self.__print_text(menuText)
    
    """
    Función: menu_hsel
    Descripción: Función que imprime la pantalla de dictado del nombre de la selección
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: sel (str, el nombre de la selección hasta ahora)
    Salidas: Mensaje en pantalla 
    """
    def __menu_hsel(self, sel):
        #Se carga el menú con su texto inicial
        menuText = "Dicte el nombre de la seleccion\n \n"
        #Se comprueba que se haya dictado al menos una letra
        if not sel.startswith("HEAR_SEL"):
            ##Se imprime la selección
            menuText += sel
            #Si la selección tiene al menos una letra entonces e imprime la opción de aceptar
            if len(sel) > 0:
                menuText += "\n \nBorrar  Cancelar  Aceptar"
            #Caso contrario se imprimen solo las opciones básicas
            else:
                menuText += "\n \nBorrar  Cancelar"
        #Caso contrario se imprimen solo las opciones básicas    
        else:
            menuText += "\n \n \nBorrar  Cancelar"
        #Se imprime el menú    
        self.__print_text(menuText)
    
    """
    Función: menu_hfsize
    Descripción: Función que imprime la pantalla de dictado del tamaño de fuente
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: size (str, el tamaño de la fuente hasta ahora)
    Salidas: Mensaje en pantalla 
    """
    def __menu_hfsize(self, size):
        #Se carga el menú con su texto inicial
        menuText = "Dicte el tamano para la fuente\n \n"
        #Se comprueba que se haya dictado al menos una letra
        if not size.startswith("HEAR_FONT_SIZE"):
            ##Se imprime la selección
            menuText += size
            #Si la selección tiene al menos una letra entonces e imprime la opción de aceptar
            if len(size) > 0:
                menuText += "\n \nBorrar  Cancelar  Aceptar"
            #Caso contrario se imprimen solo las opciones básicas
            else:
                menuText += "\n \nBorrar  Cancelar"
        #Caso contrario se imprimen solo las opciones básicas    
        else:
            menuText += "\n \n \nBorrar  Cancelar"
        #Se imprime el menú    
        self.__print_text(menuText)
        
    """
    Función: menu_ray
    Descripción: Descripción: Función que imprime la advertencia de usar el comando ray
    Autor: Christian Vargas
    Fecha de creación: 23/08/15
    Fecha de modificación: --/--/--
    Entradas: --
    Salidas: Mensaje en pantalla 
    """
    def __menu_ray(self):
        #Se carga el menú con su texto
        menuText = "Advertencia.- La renderizacion se pierde al usar \n"
        menuText += "cualquier comando, por lo que los comandos seran \n"
        menuText += "bloqueados hasta que usted diga -Continuar- \n \n        Cancelar     Aceptar"
        #Se imprime el menú    
        self.__print_text(menuText)

    """
    Función: handle_menu
    Descripción: Función que controla los menús
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: --/--/--
    Entradas: menú (str, el código del menu), mollist (dict, lista de moléculas junto con su descripcion), commandTree (CommandNode, la raíz del árbol de comandos), backupView (list, variable que guarda la posición de la cámara antes del despliegue de los menús)
    Salidas: Menú desplegado o eliminado en pantalla, posición de la pantalla antes de desplegar el menú
    """
    def __handle_menu(self, command_size=5):
        menu = self.data[command_size:]
        #Si no hay un menú cargado entonces guardamos la posición de la cámara
        if not TEXTNAME in pymol.cmd.get_names("objects"):
            self.backupView = pymol.cmd.get_view()
        #Caso contrario destruimos el último menú cargado
        else:
            pymol.cmd.delete(TEXTNAME)
        #Si el menú debe eliminarse entonces reestablecemos la cámara y la función termina    
        if menu == "clear":
            pymol.cmd.set_view(self.backupView)
            return
        #Si el menú debe mostrar las descripciones de las moléculas entonces lo manejamos con la función correspondiente y la función actual termina
        elif "SHOW_NAME" in menu:
            self.__menu_titles()
            return
        #Si estamos escuchando un código de molécula entonces manejamos el menú con la función correspondiente y la función actual termina
        elif "HEAR_MOL" in menu:
            self.__menu_mcode(menu.rsplit(' ', 1)[1])
            return
        #Si estamos escuchando una selección entonces comprobamos en qué fase de este proceso nos encontramos    
        elif "HEAR_SEL" in menu:
            #Si estamos escuchando los residuos, entonces manejamos el menú con la función correspondiente 
            if ("HEAR_RESI") in menu:
                self.__menu_hresi(menu.rsplit(' ', 1)[1])
            #Manejamos el dictado del nombre de la selección con la función correspondiente
            else:
                self.__menu_hsel(menu.rsplit(' ', 1)[1])
            #la función actual termina
            return
        #Si estamos escuchando el tamaño de una fuente entonces manejamos el menú con la función correspondiente y la función actual termina
        elif "HEAR_FONT_SIZE" in menu:
            self.__menu_hfsize(menu.rsplit(' ', 1)[1])
            return
        #Si estamos mostrando la advertencia del ray, entonces manejamos el menú con la función correspondiente y la función actual termina
        elif "PREPARE_RAY" in menu:
            self.__menu_ray()
            return
        #Se crea la variable que guardara el texto del menú
        menuText = ""
        #Se obtiene el primer menú del árbol de comandos    
        actualNode = self.commandTree.children[0]
        #Se obtiene el código del menú    
        menuCode = menu.rsplit(' ', 1)[0]
        #Se obtiene la página del menú    
        pageNumber = int(menu.rsplit(' ', 1)[1])
        #Se busca el nodo con el menú actual    
        for subCommand in menuCode.rsplit(' '):
            for page in actualNode.children:
                for son in page.children:
                    if (son.code == subCommand):
                        actualNode = son
        #Se crea el texto del menú tomando en cuenta los elementos en el menú y se imprime
        menuText += "Menu - " + actualNode.text + " (Pagina " + str(pageNumber) + ")\n "
        for i, element in enumerate(actualNode.children[pageNumber-1].children):
             menuText += "\n" + str(i+1) + ".- " + element.text
        if len(actualNode.children) > 1:
            menuText += "\n \n<- Anterior Cancelar Siguiente ->"
        else:
            menuText += "\n \n        Cancelar"
        self.__print_text(menuText)
        return
    
    """
    Función: process_command
    Descripción: Función que procesa un comando recibido por parte del Kinect y lo refleja en el Oculus
    Autor: Christian Vargas
    Fecha de creación: 02/06/15
    Fecha de modificación: --/--/--
    Entradas: command (str, comando a ser procesado)
    Salidas: Salida de video actualizada
    """
    def __process_command(self):
        command = self.data
        #Se comprueba si el usuario desea salir, de ser así regresamos falso
        if command == "QUIT":
            self.running = False
            return
        #Si se recibió un comando de desbloqueo, entonces se habilita el uso del movimiento
        elif command == "CONTINUE":
            self.moveBlocked = False
        elif command.startswith("menu"):
            #Se comprueba si el comando es de menú, de ser así se procesa con la función correspondiente
            self.__handle_menu()
        #Se comprueba si el usuario desea descargar una molécula 
        elif "fetch" in command:
            #Se le envía a PyMOL el nombre de la molécula a ser descargada (enviando el comando a partir del sexto carácter)
            pymol.cmd.fetch(self.data[6:])
            #Comprobamos que la molécula no se encontrara ya en la lista de moléculas cargadas, de ser así terminamos la función regresando verdadero
            if self.data[6:] in self.mollist:
                self.running = True
                return
            #Para comprobar que la molécula era válida buscamos el archivo pdb descargado
            if (os.path.exists(self.data[6:] + ".pdb")):
                self.sock.sendto("200".encode(), (UDP_IP, OUT_PORT))
                #Se abre el archivo para leer y buscar el titulo
                file = open(self.data[6:] + ".pdb", "r")
                #Inicializamos el texto que ir formando el título de la molécula
                title = ""
                #Esta variable permitirá saber si ya se encontró el inicio del titulo
                found = False
                #Hacemos un ciclo que se repetirá hasta el máximo de líneas a leer
                for i in range(MAXLINEREAD):
                    #Se lee la línea
                    line = file.readline()
                    #Si la línea es parte del título debe contener la palabra TITTLE, esto se comprueba en esta condición
                    if "TITLE" in line:
                        #Se inicializa j en 5 (tamaño de la palabra TITTLE) que es la variable que buscara el inicio del texto útil de la línea
                        j = 5
                        #Mientras no se haya terminado la línea y sigan apareciendo espacios blancos aumentamos j
                        while (j < len(line) and line[j] == ' '):
                            j += 1
                        #Si no se había encontrado el título ahora podemos declarar que ya se ha encontrado, de no ser así aumentamos j en 2 (las líneas que complementan el título acarrean un digito y un espacio blanco extra)
                        if (not found):
                            found = True
                        else:
                            j += 1
                        #Inicializamos k en j, k buscara el fin del texto útil de la línea
                        k = j
                        #Mientras no se haya terminado la línea y los siguientes 2 caracteres no sean espacios blancos aumentamos k
                        while (k < len(line) and line[k:k+2] != "  "):
                            k += 1
                        #Se le añade el texto útil de la línea al titulo
                        title += line[j:k]
                #Finalmente se añade el título a la lista de moléculas, el código de la molécula es la llave y el titulo el valor
                self.mollist[self.data[6:]] = title
            else:
                self.sock.sendto("500".encode(), (UDP_IP, OUT_PORT));
        #Si el usuario solicito un ray, entonces bloqueamos el uso del movimiento
        elif command == "ray":
            self.moveBlocked = True
            pymol.cmd.ray()
        #El comando viene limpio, se ejecuta en PyMOL
        else:
            if "select" in command:
                if re.search("select \w+ ,resi \d+(\+\d+)*",command):
                    self.sock.sendto("200".encode(),(UDP_IP,OUT_PORT))
                    print("Enviado 200 a c#")
                else:
                    self.sock.sendto("500".encode(),(UDP_IP,OUT_PORT))
                    print("Enviado 500 a c#")
            pymol.cmd.do(command)
        self.running = True

    """
    Función: main
    Descripción: Función principal, inicializa y comprueba el hardware, carga el visualizador de moléculas y controla la cámara en base al movimiento del OVR
    Autor: Christian Vargas
    Fecha de creación: 31/05/15
    Fecha de modificación: 02/06/15
    Entradas: --
    Salidas: Visualización en el Oculus de moléculas con controles de orientación integrados
    """
    def main(self): 
        while self.running:
            #Si no estamos depurando y el movimiento no esta bloqueado, movemos los objetos de acuerdo al movimiento del Oculus
            if not self.debug and not self.moveBlocked:
                #Se obtiene la posición actual del OVR
                ss = ovrsdk.ovrHmd_GetSensorState(self.hmd, ovrsdk.ovr_GetTimeInSeconds())
                pose = ss.Predicted.Pose
                #Valores de posición actuales del OVR
                currp = [pose.Orientation.x,pose.Orientation.y]
                #Movemos la cámara del visualizador de moléculas en sentido al movimiento del OVR
                pymol.cmd.move('x',(currp[1]-self.prevp[1])*self.ovr_sensitivity)
                pymol.cmd.move('y',-(currp[0]-self.prevp[0])*self.ovr_sensitivity)
                #Actualizamos los valores de posición anteriores del OVR
                self.prevp = currp
            #Comprobamos si hay comandos por recibir, en caso de no existir alguno, se ignora el error que generaría el sistema operativo.
            try:
                self.data = self.sock.recv(1024)
            except socket.error:
                pass
            else:
                self.__process_command()
            #Dormimos el programa para disminuir el uso de CPU
            time.sleep(SLEEPTIME)
        pymol.cmd.quit()

if __name__ == "__main__":
    #Comprobamos si el programa se lanzó en modo de depuración
    if len(sys.argv) > 1:
        #Un 1 como primer argumento significa que el programa debe estar en modo depuración
        if (sys.argv[1] == '1'):
            mol = RealMOL(True)
        else:
            mol = RealMOL()
    else:
        mol = RealMOL()
    mol.main()