/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.FileEdit
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Class managing a button for a single file or directory in the <see cref="SelectFileDialog"/>
    /// </summary>
    public class SelectFileDialogFileButton : MonoBehaviour
    {
        /// <summary>
        /// Type of the file button
        /// </summary>
        private enum ButtonType
        {
            /// <summary>
            /// Uninitialized file button
            /// </summary>
            None,
            
            /// <summary>
            /// File button points on a file not a directory
            /// </summary>
            File,
            
            /// <summary>
            /// File button points on a directory
            /// </summary>
            Directory
        }

        /// <summary>
        /// This button's type
        /// </summary>
        private ButtonType buttonType;
        
        /// <summary>
        /// Reference to this UI button object
        /// </summary>
        public Button button;
        
        /// <summary>
        /// Reference to the title text object
        /// </summary>
        public Text title;
        
        /// <summary>
        /// Reference to the icon available in the text font
        /// </summary>
        public Text Icon;

        /// <summary>
        /// Folder icon unicode that will be used for a directory button
        /// </summary>
        public string FolderIcon;
        
        /// <summary>
        /// Folder icon unicode that will be used for a file button
        /// </summary>
        public string FileIcon;

        /// <summary>
        /// Parent <see cref="SelectFileDialog"/>
        /// </summary>
        public SelectFileDialog ParentDialog { get; set; }

        /// <summary>
        /// Marks this button as a file type
        /// </summary>
        /// <param name="fileName">Name of the corresponding file</param>
        public void MarkAsFile(string fileName)
        {
            buttonType = ButtonType.File;
            title.text = fileName;
            Icon.text = FileIcon;
        }

        /// <summary>
        /// Marks this button as a directory type
        /// </summary>
        /// <param name="directoryName">Name of the corresponding directory</param>
        public void MarkAsDirectory(string directoryName)
        {
            buttonType = ButtonType.Directory;
            title.text = directoryName + Path.DirectorySeparatorChar;
            Icon.text = FolderIcon;
        }

        /// <summary>
        /// Marks this button as currently selected
        /// </summary>
        public void Select()
        {
            button.interactable = false;
        }

        /// <summary>
        /// Unmarks this button as it is no longer selected
        /// </summary>
        public void Unselect()
        {
            button.interactable = true;
        }

        /// <summary>
        /// Button was clicked and will invoke proper method in <see cref="ParentDialog"/> depending on the <see cref="buttonType"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Invalid button type</exception>
        public void Clicked()
        {
            switch (buttonType)
            {
                case ButtonType.None:
                    break;
                case ButtonType.File:
                    ParentDialog.SelectFile(this);
                    break;
                case ButtonType.Directory:
                    ParentDialog.SelectDirectoryPath($"{ParentDialog.DirectoryPath}{title.text}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}