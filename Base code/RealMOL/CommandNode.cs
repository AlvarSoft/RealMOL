using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RealMOL
{
    public enum Types { Root, Menu, Page, Command };
    public class CommandNode
    {
        public Types type { get; set; }
        public string text { get; set; }
        public string code { get; set; }

        public ICollection<CommandNode> children { get; set; }

        /*
         * Función: CommandNode
         * Descripción: Constructor de un nodo de árbol de comando
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: type (Types, el tipo de nodo), text (string, el texto del nodo), code (string, el código del nodo)
         * Salidas: (CommandNode, el nodo de árbol de comando con datos validos)
         */
        public CommandNode(Types type, string text, string code)
        {
            this.type = type;
            this.text = text;
            this.code = code;
            this.children = new LinkedList<CommandNode>();
        }

        /*
         * Función: AddChild
         * Descripción: Función que añade un hijo a un nodo de árbol de comandos
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: childType (Types, el tipo de nodo), childText (string, el texto del nodo), childCode (string, el código del nodo)
         * Salidas: (CommandNode, el hijo del nodo de árbol de comando con datos validos)
         */
        private CommandNode AddChild(Types childType, string childText, string childCode)
        {
            CommandNode childNode = new CommandNode(childType, childText, childCode);
            this.children.Add(childNode);
            return childNode;
        }

        /*
         * Función: AddTreeChilds
         * Descripción: Función recursiva que recorre el archivo XML y llena el árbol de comandos
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: Nodo inicial
         * Salidas: Árbol cargado con datos del XML
         */
        private void AddTreeChilds(CommandNode parent, XmlNode xmlNode)
        {
            CommandNode newNode;
            if (xmlNode.Name == "menu")
            {
                newNode = parent.AddChild(Types.Menu, xmlNode.ChildNodes[0].InnerText, xmlNode.ChildNodes[1].InnerText);
                for (int i = 2; i < xmlNode.ChildNodes.Count; i++)
                {
                    AddTreeChilds(newNode, xmlNode.ChildNodes[i]);
                }
            }
            else if (xmlNode.Name == "page")
            {
                newNode = parent.AddChild(Types.Page, "", "");
                for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
                {
                    AddTreeChilds(newNode, xmlNode.ChildNodes[i]);
                }
            }
            else if (xmlNode.Name == "command")
            {
                newNode = parent.AddChild(Types.Command, xmlNode.ChildNodes[0].InnerText, xmlNode.ChildNodes[1].InnerText);
            }
        }

        /*
         * Función: CreateTree
         * Descripción: Función recursiva que recorre el archivo XML y llena el árbol de comandos
         * Autor: Christian Vargas
         * Fecha de creación: 30/07/15
         * Fecha de modificación: --/--/--
         * Entradas: Nodo inicial
         * Salidas: Árbol cargado con datos del XML
         */
        public void CreateTree(string file)
        {
            XmlDocument xml_doc = new XmlDocument();
            xml_doc.Load(file);
            AddTreeChilds(this, xml_doc.DocumentElement);
        }
    }
}